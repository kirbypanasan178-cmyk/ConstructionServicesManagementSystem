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
    public class DashboardServiceTests
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

        // Matches the real Booking/Billing/Payment shapes from BookingService/PaymentService.

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

        // Billing.BookingId is a required FK, so every Billing needs a real
        // Booking behind it (dashboard queries hit Billings directly, but the
        // FK still has to point somewhere realistic).
        private static Billing MakeBilling(
            Booking booking,
            decimal totalAmount,
            BillingStatus status,
            decimal amountPaid = 0m,
            DateTime? createdAt = null)
        {
            return new Billing
            {
                Booking = booking,
                BookingId = booking.Id,
                BillingNumber = $"BILL-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                TotalAmount = totalAmount,
                AmountPaid = amountPaid,
                Balance = totalAmount - amountPaid,
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }

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

        // ---------- TotalClients ----------

        [Fact]
        public async Task GetDashboardAsync_ReturnsCorrectTotalClients()
        {
            using var context = CreateContext();
            context.Clients.AddRange(MakeClient("Anna Cruz"), MakeClient("Bob Diaz"), MakeClient("Carl Evans"));
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(3, result.TotalClients);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsZeroTotalClients_WhenNoneExist()
        {
            using var context = CreateContext();
            var service = new DashboardService(context);

            var result = await service.GetDashboardAsync();

            Assert.Equal(0, result.TotalClients);
        }

        // ---------- UpcomingBookings / UpcomingSchedule ----------

        [Fact]
        public async Task GetDashboardAsync_CountsOnlyBookingsFromTodayOnward()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var today = DateTime.Today;
            context.Bookings.AddRange(
                MakeBooking(client, today.AddDays(-1)),  // past, excluded
                MakeBooking(client, today),               // today, included
                MakeBooking(client, today.AddDays(3))     // future, included
            );
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(2, result.UpcomingBookings);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsUpcomingSchedule_OrderedByVisitDate_LimitedToFive()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var today = DateTime.Today;
            // 6 upcoming bookings, out of order, plus 1 past booking that must be excluded.
            context.Bookings.AddRange(
                MakeBooking(client, today.AddDays(-5)),  // past, excluded
                MakeBooking(client, today.AddDays(5)),
                MakeBooking(client, today.AddDays(1)),
                MakeBooking(client, today.AddDays(3)),
                MakeBooking(client, today.AddDays(2)),
                MakeBooking(client, today.AddDays(4)),
                MakeBooking(client, today.AddDays(6))
            );
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(5, result.UpcomingSchedule.Count);
            var expectedOrder = new[] { 1, 2, 3, 4, 5 };
            var actualOrder = result.UpcomingSchedule.Select(b => (b.VisitDate - today).Days).ToArray();
            Assert.Equal(expectedOrder, actualOrder);
        }

        [Fact]
        public async Task GetDashboardAsync_UpcomingSchedule_IncludesClientNavigationProperty()
        {
            using var context = CreateContext();
            var client = MakeClient("Anna Cruz");
            context.Clients.Add(client);
            context.Bookings.Add(MakeBooking(client, DateTime.Today.AddDays(1)));
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Single(result.UpcomingSchedule);
            Assert.NotNull(result.UpcomingSchedule[0].Client);
            Assert.Equal("Anna Cruz", result.UpcomingSchedule[0].Client!.FullName);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsEmptySchedule_WhenNoUpcomingBookings()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            context.Bookings.Add(MakeBooking(client, DateTime.Today.AddDays(-2)));
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Empty(result.UpcomingSchedule);
            Assert.Equal(0, result.UpcomingBookings);
        }

        // ---------- TotalRevenue ----------

        [Fact]
        public async Task GetDashboardAsync_ReturnsCorrectTotalRevenue_SumOfAllPayments()
        {
            using var context = CreateContext();
            var client = MakeClient();
            var booking = MakeBooking(client, DateTime.Today.AddDays(-10), 1000m);
            var billing = MakeBilling(booking, 1000m, BillingStatus.Paid);
            context.Billings.Add(billing);
            context.Payments.AddRange(
                MakePayment(billing, 500m, DateTime.Today),
                MakePayment(billing, 250.50m, DateTime.Today.AddDays(-1))
            );
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(750.50m, result.TotalRevenue);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsZeroTotalRevenue_WhenNoPaymentsExist()
        {
            using var context = CreateContext();
            var service = new DashboardService(context);

            var result = await service.GetDashboardAsync();

            Assert.Equal(0m, result.TotalRevenue);
        }

        // ---------- RecentPayments ----------

        [Fact]
        public async Task GetDashboardAsync_ReturnsRecentPayments_OrderedByPaymentDateDescending_LimitedToFive()
        {
            using var context = CreateContext();
            var client = MakeClient();
            var booking = MakeBooking(client, DateTime.Today.AddDays(-10), 1000m);
            var billing = MakeBilling(booking, 1000m, BillingStatus.Paid);
            context.Billings.Add(billing);

            var today = DateTime.Today;
            for (int i = 0; i < 7; i++)
            {
                context.Payments.Add(MakePayment(billing, 100m, today.AddDays(-i)));
            }
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(5, result.RecentPayments.Count);
            var expectedOrder = new[] { 0, -1, -2, -3, -4 }.Select(d => today.AddDays(d)).ToArray();
            var actualOrder = result.RecentPayments.Select(p => p.PaymentDate).ToArray();
            Assert.Equal(expectedOrder, actualOrder);
        }

        [Fact]
        public async Task GetDashboardAsync_RecentPayments_IncludesBillingNavigationProperty()
        {
            using var context = CreateContext();
            var client = MakeClient();
            var booking = MakeBooking(client, DateTime.Today.AddDays(-10), 750m);
            var billing = MakeBilling(booking, 750m, BillingStatus.Unpaid);
            context.Billings.Add(billing);
            context.Payments.Add(MakePayment(billing, 200m, DateTime.Today));
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Single(result.RecentPayments);
            Assert.NotNull(result.RecentPayments[0].Billing);
            Assert.Equal(750m, result.RecentPayments[0].Billing!.TotalAmount);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsEmptyRecentPayments_WhenNonePaid()
        {
            using var context = CreateContext();
            var service = new DashboardService(context);

            var result = await service.GetDashboardAsync();

            Assert.Empty(result.RecentPayments);
        }

        // ---------- PendingAmount ----------

        [Fact]
        public async Task GetDashboardAsync_ReturnsCorrectPendingAmount_ExcludesPaidBillings()
        {
            using var context = CreateContext();
            var client = MakeClient();
            var today = DateTime.Today;
            context.Billings.AddRange(
                MakeBilling(MakeBooking(client, today.AddDays(-1), 1000m), 1000m, BillingStatus.Paid),          // excluded
                MakeBilling(MakeBooking(client, today.AddDays(-2), 500m), 500m, BillingStatus.Unpaid),          // included
                MakeBilling(MakeBooking(client, today.AddDays(-3), 250m), 250m, BillingStatus.PartiallyPaid)    // included
            );
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(750m, result.PendingAmount);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsZeroPendingAmount_WhenAllBillingsPaid()
        {
            using var context = CreateContext();
            var client = MakeClient();
            var today = DateTime.Today;
            context.Billings.AddRange(
                MakeBilling(MakeBooking(client, today.AddDays(-1), 1000m), 1000m, BillingStatus.Paid),
                MakeBilling(MakeBooking(client, today.AddDays(-2), 500m), 500m, BillingStatus.Paid)
            );
            await context.SaveChangesAsync();

            var service = new DashboardService(context);
            var result = await service.GetDashboardAsync();

            Assert.Equal(0m, result.PendingAmount);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsZeroPendingAmount_WhenNoBillingsExist()
        {
            using var context = CreateContext();
            var service = new DashboardService(context);

            var result = await service.GetDashboardAsync();

            Assert.Equal(0m, result.PendingAmount);
        }
    }
}