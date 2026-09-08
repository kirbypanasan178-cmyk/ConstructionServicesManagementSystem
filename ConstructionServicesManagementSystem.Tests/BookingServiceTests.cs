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
    public class BookingServiceTests
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

        // ASSUMPTION: Service model isn't visible to me — inferred only from
        // what BookingService.CreateAsync reads/writes: Id, HourlyRate, IsActive.
        // Adjust if your real model has other required properties.
        private static Service MakeService(decimal hourlyRate = 500m, bool isActive = true, string name = "Masonry")
        {
            return new Service
            {
                Name = name,
                HourlyRate = hourlyRate,
                IsActive = isActive
            };
        }

        // ASSUMPTION: BookingDetail model inferred from CreateAsync usage:
        // ServiceId, HoursRendered (input), HourlyRate/Amount (calculated by the service).
        private static BookingDetail MakeBookingDetailInput(int serviceId, decimal hoursRendered)
        {
            return new BookingDetail
            {
                ServiceId = serviceId,
                HoursRendered = hoursRendered
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
            string? billingNumber = null)
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
                CreatedAt = DateTime.UtcNow
            };
        }

        // ---------- GetAllAsync (no paging) ----------

        [Fact]
        public async Task GetAllAsync_ReturnsAllBookings_OrderedByVisitDateDescending()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var today = DateTime.Today;
            context.Bookings.AddRange(
                MakeBooking(client, today.AddDays(-5)),
                MakeBooking(client, today.AddDays(1)),
                MakeBooking(client, today.AddDays(-1))
            );
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync();

            Assert.Equal(3, result.Count);
            var expectedOrder = new[] { 1, -1, -5 };
            Assert.Equal(expectedOrder, result.Select(b => (b.VisitDate - today).Days).ToArray());
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoBookingsExist()
        {
            using var context = CreateContext();
            var service = new BookingService(context);

            var result = await service.GetAllAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_IncludesClientAndBilling()
        {
            using var context = CreateContext();
            var client = MakeClient("Anna Cruz");
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today, 1000m);
            context.Bookings.Add(booking);
            context.Billings.Add(MakeBilling(booking, 1000m, BillingStatus.Unpaid));
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync();

            Assert.Single(result);
            Assert.NotNull(result[0].Client);
            Assert.Equal("Anna Cruz", result[0].Client!.FullName);
            Assert.NotNull(result[0].Billing);
        }

        // ---------- GetAllAsync (paged + search + status filter) ----------

        [Fact]
        public async Task GetAllAsync_Paged_ReturnsCorrectPage_OrderedByVisitDateDescending()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var today = DateTime.Today;
            context.Bookings.AddRange(
                MakeBooking(client, today.AddDays(1)),
                MakeBooking(client, today.AddDays(2)),
                MakeBooking(client, today.AddDays(3)),
                MakeBooking(client, today.AddDays(4)),
                MakeBooking(client, today.AddDays(5))
            );
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync(pageNumber: 2, pageSize: 2);

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { 3, 2 }, result.Select(b => (b.VisitDate - today).Days).ToArray());
        }

        [Fact]
        public async Task GetAllAsync_Paged_FiltersBySearch_MatchingClientFullName()
        {
            using var context = CreateContext();
            var clientA = MakeClient("Anna Cruz");
            var clientB = MakeClient("Bob Diaz");
            context.Clients.AddRange(clientA, clientB);

            context.Bookings.AddRange(
                MakeBooking(clientA, DateTime.Today),
                MakeBooking(clientB, DateTime.Today)
            );
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync(1, 10, search: "Anna");

            Assert.Single(result);
            Assert.Equal("Anna Cruz", result[0].Client!.FullName);
        }

        [Fact]
        public async Task GetAllAsync_Paged_FiltersBySearch_MatchingBillingNumber()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var bookingWithMatch = MakeBooking(client, DateTime.Today);
            var bookingWithoutMatch = MakeBooking(client, DateTime.Today.AddDays(1));
            context.Bookings.AddRange(bookingWithMatch, bookingWithoutMatch);

            context.Billings.AddRange(
                MakeBilling(bookingWithMatch, 500m, BillingStatus.Unpaid, billingNumber: "BILL-TARGET-001"),
                MakeBilling(bookingWithoutMatch, 500m, BillingStatus.Unpaid, billingNumber: "BILL-OTHER-002")
            );
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync(1, 10, search: "TARGET");

            Assert.Single(result);
            Assert.Equal("BILL-TARGET-001", result[0].Billing!.BillingNumber);
        }

        [Fact]
        public async Task GetAllAsync_Paged_FiltersByBillingStatus()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);

            var unpaidBooking = MakeBooking(client, DateTime.Today);
            var paidBooking = MakeBooking(client, DateTime.Today.AddDays(1));
            context.Bookings.AddRange(unpaidBooking, paidBooking);

            context.Billings.AddRange(
                MakeBilling(unpaidBooking, 500m, BillingStatus.Unpaid),
                MakeBilling(paidBooking, 500m, BillingStatus.Paid)
            );
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync(1, 10, billingStatus: BillingStatus.Paid);

            Assert.Single(result);
            Assert.Equal(BillingStatus.Paid, result[0].Billing!.Status);
        }

        [Fact]
        public async Task GetAllAsync_Paged_ReturnsEmptyList_WhenSearchMatchesNothing()
        {
            using var context = CreateContext();
            var client = MakeClient("Anna Cruz");
            context.Clients.Add(client);
            context.Bookings.Add(MakeBooking(client, DateTime.Today));
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetAllAsync(1, 10, search: "no-such-client");

            Assert.Empty(result);
        }

        // ---------- GetByIdAsync ----------

        [Fact]
        public async Task GetByIdAsync_ReturnsBooking_WhenExists()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var booking = MakeBooking(client, DateTime.Today);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.GetByIdAsync(booking.Id);

            Assert.NotNull(result);
            Assert.Equal(booking.Id, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            using var context = CreateContext();
            var service = new BookingService(context);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        // ---------- IsDateBookedAsync ----------

        [Fact]
        public async Task IsDateBookedAsync_ReturnsTrue_WhenDateIsBooked()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var visitDate = new DateTime(2026, 10, 15);
            context.Bookings.Add(MakeBooking(client, visitDate));
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var result = await service.IsDateBookedAsync(new DateTime(2026, 10, 15));

            Assert.True(result);
        }

        [Fact]
        public async Task IsDateBookedAsync_IgnoresTimeComponent()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            context.Bookings.Add(MakeBooking(client, new DateTime(2026, 10, 15, 9, 0, 0)));
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            // Same calendar day, different time — should still count as booked.
            var result = await service.IsDateBookedAsync(new DateTime(2026, 10, 15, 16, 30, 0));

            Assert.True(result);
        }

        [Fact]
        public async Task IsDateBookedAsync_ReturnsFalse_WhenDateIsNotBooked()
        {
            using var context = CreateContext();
            var service = new BookingService(context);

            var result = await service.IsDateBookedAsync(new DateTime(2026, 12, 25));

            Assert.False(result);
        }

        // ---------- CreateAsync ----------

        [Fact]
        public async Task CreateAsync_Throws_WhenDateAlreadyBooked()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var visitDate = new DateTime(2026, 11, 1);
            context.Bookings.Add(MakeBooking(client, visitDate));
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var svc = MakeService();
            context.Services.Add(svc);
            await context.SaveChangesAsync();

            var details = new List<BookingDetail> { MakeBookingDetailInput(svc.Id, 4) };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(client.Id, visitDate, details));
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenBookingDetailsIsNull()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var service = new BookingService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(client.Id, DateTime.Today.AddDays(1), null!));
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenBookingDetailsIsEmpty()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var service = new BookingService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(client.Id, DateTime.Today.AddDays(1), new List<BookingDetail>()));
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenServiceIsInactive()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var inactiveService = MakeService(isActive: false);
            context.Services.Add(inactiveService);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var details = new List<BookingDetail> { MakeBookingDetailInput(inactiveService.Id, 4) };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(client.Id, DateTime.Today.AddDays(1), details));
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenServiceDoesNotExist()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var details = new List<BookingDetail> { MakeBookingDetailInput(999, 4) };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(client.Id, DateTime.Today.AddDays(1), details));
        }

        [Fact]
        public async Task CreateAsync_Throws_WhenHoursRenderedIsZeroOrNegative()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var svc = MakeService();
            context.Services.Add(svc);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var details = new List<BookingDetail> { MakeBookingDetailInput(svc.Id, 0) };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(client.Id, DateTime.Today.AddDays(1), details));
        }

        [Fact]
        public async Task CreateAsync_CalculatesDetailAmounts_AndBookingTotal_Correctly()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var svcA = MakeService(hourlyRate: 500m, name: "Masonry");
            var svcB = MakeService(hourlyRate: 300m, name: "Plumbing");
            context.Services.AddRange(svcA, svcB);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var details = new List<BookingDetail>
            {
                MakeBookingDetailInput(svcA.Id, 4),  // 4 * 500 = 2000
                MakeBookingDetailInput(svcB.Id, 2)   // 2 * 300 = 600
            };

            var result = await service.CreateAsync(client.Id, DateTime.Today.AddDays(1), details);

            Assert.Equal(2600m, result.TotalAmount);
            Assert.Equal(500m, result.BookingDetails.First(d => d.ServiceId == svcA.Id).HourlyRate);
            Assert.Equal(2000m, result.BookingDetails.First(d => d.ServiceId == svcA.Id).Amount);
            Assert.Equal(300m, result.BookingDetails.First(d => d.ServiceId == svcB.Id).HourlyRate);
            Assert.Equal(600m, result.BookingDetails.First(d => d.ServiceId == svcB.Id).Amount);
        }

        [Fact]
        public async Task CreateAsync_CreatesAssociatedUnpaidBilling_MatchingBookingTotal()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var svc = MakeService(hourlyRate: 500m);
            context.Services.Add(svc);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var details = new List<BookingDetail> { MakeBookingDetailInput(svc.Id, 4) };

            var booking = await service.CreateAsync(client.Id, DateTime.Today.AddDays(1), details);

            var billing = await context.Billings.FirstOrDefaultAsync(b => b.BookingId == booking.Id);

            Assert.NotNull(billing);
            Assert.Equal(BillingStatus.Unpaid, billing!.Status);
            Assert.Equal(2000m, billing.TotalAmount);
            Assert.Equal(0m, billing.AmountPaid);
            Assert.Equal(2000m, billing.Balance);
            Assert.StartsWith("BILL-", billing.BillingNumber);
        }

        [Fact]
        public async Task CreateAsync_PersistsBookingToDatabase()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            var svc = MakeService();
            context.Services.Add(svc);
            await context.SaveChangesAsync();

            var service = new BookingService(context);
            var visitDate = DateTime.Today.AddDays(2);
            var details = new List<BookingDetail> { MakeBookingDetailInput(svc.Id, 3) };

            var created = await service.CreateAsync(client.Id, visitDate, details);

            var stored = await context.Bookings.FindAsync(created.Id);
            Assert.NotNull(stored);
            Assert.Equal(visitDate, stored!.VisitDate);
            Assert.Equal(client.Id, stored.ClientId);
        }
    }
}