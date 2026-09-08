using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConstructionServicesManagementSystem.Tests.Services
{
    public class PaymentServiceTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static Client MakeClient(string fullName = "John Doe")
        {
            return new Client
            {
                FullName = fullName,
                Email = $"{fullName.Replace(" ", "").ToLower()}@example.com",
                PhoneNumber = "0912-345-6789",
                Address = "Davao City"
            };
        }

        private static Booking MakeBooking(Client client, DateTime visitDate, decimal totalAmount = 0m)
        {
            return new Booking
            {
                Client = client,
                ClientId = client.Id,
                VisitDate = visitDate,
                TotalAmount = totalAmount,
                BookingDetails = new List<BookingDetail>()
            };
        }

        private static Billing MakeBilling(
            Booking booking,
            decimal totalAmount,
            BillingStatus status,
            decimal amountPaid = 0m,
            string? billingNumber = null,
            DateTime? createdAt = null)
        {
            return new Billing
            {
                Booking = booking,
                BookingId = booking.Id,
                BillingNumber = billingNumber ?? $"BILL-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                TotalAmount = totalAmount,
                AmountPaid = amountPaid,
                Balance = totalAmount - amountPaid,
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }

        // ASSUMPTION: PaymentMethod enum isn't visible to me. I'm defaulting to
        // PaymentMethod.Cash — swap this (and the enum reference) for whatever
        // your real enum defines if it doesn't have a Cash member.
        private static Payment MakePayment(
            Billing billing,
            decimal amount,
            DateTime paymentDate,
            PaymentMethod paymentMethod = PaymentMethod.Cash)
        {
            return new Payment
            {
                Billing = billing,
                BillingId = billing.Id,
                Amount = amount,
                PaymentDate = paymentDate,
                PaymentMethod = paymentMethod,
                ReferenceNumber = $"PAY-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
            };
        }

        // ---------- GetPendingBillingsAsync (no paging) ----------

        [Fact]
        public async Task GetPendingBillingsAsync_ReturnsOnlyPositiveBalanceBillings_OrderedByCreatedAtDescending()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var bookingA = MakeBooking(client, DateTime.Today.AddDays(-3), 500m);
            var bookingB = MakeBooking(client, DateTime.Today.AddDays(-2), 800m);
            var bookingC = MakeBooking(client, DateTime.Today.AddDays(-1), 300m);
            context.Bookings.AddRange(bookingA, bookingB, bookingC);

            var now = DateTime.UtcNow;
            context.Billings.AddRange(
                MakeBilling(bookingA, 500m, BillingStatus.Unpaid, createdAt: now.AddHours(-3)),
                MakeBilling(bookingB, 800m, BillingStatus.PartiallyPaid, amountPaid: 200m, createdAt: now.AddHours(-1)),
                MakeBilling(bookingC, 300m, BillingStatus.Paid, amountPaid: 300m, createdAt: now) // fully paid, excluded
            );
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetPendingBillingsAsync();

            Assert.Equal(2, result.Count);
            // Most recently created first.
            Assert.Equal(800m, result[0].TotalAmount);
            Assert.Equal(500m, result[1].TotalAmount);
        }

        [Fact]
        public async Task GetPendingBillingsAsync_ReturnsEmptyList_WhenAllBillingsArePaidOff()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            context.Billings.Add(MakeBilling(booking, 500m, BillingStatus.Paid, amountPaid: 500m));
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetPendingBillingsAsync();

            Assert.Empty(result);
        }

        // ---------- GetPendingBillingsAsync (paged + search) ----------

        [Fact]
        public async Task GetPendingBillingsAsync_Paged_ReturnsCorrectPage()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var now = DateTime.UtcNow;
            for (int i = 0; i < 5; i++)
            {
                var booking = MakeBooking(client, DateTime.Today.AddDays(-i), 100m);
                context.Bookings.Add(booking);
                context.Billings.Add(MakeBilling(booking, 100m, BillingStatus.Unpaid, createdAt: now.AddMinutes(-i)));
            }
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetPendingBillingsAsync(pageNumber: 2, pageSize: 2);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetPendingBillingsAsync_Paged_FiltersBySearch_MatchingBillingNumber()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            context.Billings.Add(MakeBilling(booking, 500m, BillingStatus.Unpaid, billingNumber: "BILL-TARGET-001"));
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetPendingBillingsAsync(1, 10, search: "TARGET");

            Assert.Single(result);
            Assert.Equal("BILL-TARGET-001", result[0].BillingNumber);
        }

        [Fact]
        public async Task GetPendingBillingsAsync_Paged_FiltersBySearch_MatchingClientFullName()
        {
            using var context = CreateContext();
            var clientA = MakeClient("Anna Cruz");
            var clientB = MakeClient("Bob Diaz");
            context.Clients.AddRange(clientA, clientB);

            var bookingA = MakeBooking(clientA, DateTime.Today, 500m);
            var bookingB = MakeBooking(clientB, DateTime.Today, 500m);
            context.Bookings.AddRange(bookingA, bookingB);

            context.Billings.AddRange(
                MakeBilling(bookingA, 500m, BillingStatus.Unpaid),
                MakeBilling(bookingB, 500m, BillingStatus.Unpaid)
            );
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetPendingBillingsAsync(1, 10, search: "Anna");

            Assert.Single(result);
            Assert.Equal("Anna Cruz", result[0].Booking!.Client!.FullName);
        }

        [Fact]
        public async Task GetPendingBillingsAsync_Paged_ExcludesFullyPaidBillings()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            context.Billings.Add(MakeBilling(booking, 500m, BillingStatus.Paid, amountPaid: 500m));
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetPendingBillingsAsync(1, 10);

            Assert.Empty(result);
        }

        // ---------- GetBillingByIdAsync ----------

        [Fact]
        public async Task GetBillingByIdAsync_ReturnsBilling_WithBookingAndPayments_WhenExists()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.PartiallyPaid, amountPaid: 200m);
            context.Billings.Add(billing);
            context.Payments.Add(MakePayment(billing, 200m, DateTime.UtcNow));
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.GetBillingByIdAsync(billing.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.Booking);
            Assert.Single(result.Payments);
        }

        [Fact]
        public async Task GetBillingByIdAsync_ReturnsNull_WhenNotExists()
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            var result = await service.GetBillingByIdAsync(999);

            Assert.Null(result);
        }

        // ---------- ProcessPaymentAsync ----------

        [Fact]
        public async Task ProcessPaymentAsync_ReturnsNull_WhenBillingDoesNotExist()
        {
            using var context = CreateContext();
            var service = new PaymentService(context);

            var result = await service.ProcessPaymentAsync(999, 100m, PaymentMethod.Cash);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public async Task ProcessPaymentAsync_ReturnsNull_WhenAmountIsZeroOrNegative(decimal amount)
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.ProcessPaymentAsync(billing.Id, amount, PaymentMethod.Cash);

            Assert.Null(result);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ReturnsNull_WhenAmountExceedsBalance()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var result = await service.ProcessPaymentAsync(billing.Id, 600m, PaymentMethod.Cash);

            Assert.Null(result);
        }

        [Fact]
        public async Task ProcessPaymentAsync_PartialPayment_SetsStatusToPartiallyPaid_AndUpdatesBalance()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var payment = await service.ProcessPaymentAsync(billing.Id, 200m, PaymentMethod.Cash);

            Assert.NotNull(payment);
            Assert.Equal(200m, payment!.Amount);
            Assert.StartsWith("PAY-", payment.ReferenceNumber);

            var updatedBilling = await context.Billings.FindAsync(billing.Id);
            Assert.Equal(200m, updatedBilling!.AmountPaid);
            Assert.Equal(300m, updatedBilling.Balance);
            Assert.Equal(BillingStatus.PartiallyPaid, updatedBilling.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_FullPayment_SetsStatusToPaid_AndZeroesBalance()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var payment = await service.ProcessPaymentAsync(billing.Id, 500m, PaymentMethod.Cash);

            Assert.NotNull(payment);

            var updatedBilling = await context.Billings.FindAsync(billing.Id);
            Assert.Equal(500m, updatedBilling!.AmountPaid);
            Assert.Equal(0m, updatedBilling.Balance);
            Assert.Equal(BillingStatus.Paid, updatedBilling.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_SecondPartialPayment_CompletesBalance_AndMarksPaid()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.PartiallyPaid, amountPaid: 200m);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            // Remaining balance is 300 — pay it off exactly.
            var payment = await service.ProcessPaymentAsync(billing.Id, 300m, PaymentMethod.Cash);

            Assert.NotNull(payment);

            var updatedBilling = await context.Billings.FindAsync(billing.Id);
            Assert.Equal(500m, updatedBilling!.AmountPaid);
            Assert.Equal(0m, updatedBilling.Balance);
            Assert.Equal(BillingStatus.Paid, updatedBilling.Status);
        }

        [Fact]
        public async Task ProcessPaymentAsync_PersistsPaymentToDatabase()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 500m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 500m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var payment = await service.ProcessPaymentAsync(billing.Id, 150m, PaymentMethod.Cash);

            var storedPayment = await context.Payments.FindAsync(payment!.Id);
            Assert.NotNull(storedPayment);
            Assert.Equal(billing.Id, storedPayment!.BillingId);
            Assert.Equal(150m, storedPayment.Amount);
        }

        [Fact]
        public async Task ProcessPaymentAsync_GeneratesUniqueReferenceNumbers_AcrossMultiplePayments()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 1000m);
            context.Bookings.Add(booking);
            var billing = MakeBilling(booking, 1000m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            await context.SaveChangesAsync();

            var service = new PaymentService(context);
            var paymentOne = await service.ProcessPaymentAsync(billing.Id, 100m, PaymentMethod.Cash);
            var paymentTwo = await service.ProcessPaymentAsync(billing.Id, 100m, PaymentMethod.Cash);

            Assert.NotNull(paymentOne);
            Assert.NotNull(paymentTwo);
            Assert.NotEqual(paymentOne!.ReferenceNumber, paymentTwo!.ReferenceNumber);
        }
    }
}