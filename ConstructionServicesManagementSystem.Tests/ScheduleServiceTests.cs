using System;
using System.Linq;
using System.Threading.Tasks;
using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConstructionServicesManagementSystem.Tests.Services
{
    public class ScheduleServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        // NOTE: Assumes Booking has Id, VisitDate, ClientId/Client nav,
        // and BookingDetails (each with a ServiceId/Service nav).
        // Adjust the object initializers below if your Client/BookingDetail
        // constructors/required fields differ.

        [Fact]
        public async Task GetWeeklyScheduleAsync_ReturnsOnlyBookingsWithinWeek()
        {
            using var context = CreateContext(nameof(GetWeeklyScheduleAsync_ReturnsOnlyBookingsWithinWeek));

            var client = new Client { Id = 1, FullName = "Acme Corp" };
            var svc = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            context.Clients.Add(client);
            context.Services.Add(svc);

            var weekStart = new DateTime(2026, 9, 7); // a Monday

            context.Bookings.AddRange(
                new Booking
                {
                    Id = 1,
                    ClientId = client.Id,
                    VisitDate = weekStart.AddDays(1), // inside the week
                    BookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail { ServiceId = svc.Id }
                    }
                },
                new Booking
                {
                    Id = 2,
                    ClientId = client.Id,
                    VisitDate = weekStart.AddDays(-1), // before the week
                    BookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail { ServiceId = svc.Id }
                    }
                },
                new Booking
                {
                    Id = 3,
                    ClientId = client.Id,
                    VisitDate = weekStart.AddDays(7), // exactly at the boundary, exclusive
                    BookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail { ServiceId = svc.Id }
                    }
                }
            );
            await context.SaveChangesAsync();

            var scheduleService = new ScheduleService(context);
            var result = await scheduleService.GetWeeklyScheduleAsync(weekStart);

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task GetWeeklyScheduleAsync_ReturnsResultsOrderedByVisitDate()
        {
            using var context = CreateContext(nameof(GetWeeklyScheduleAsync_ReturnsResultsOrderedByVisitDate));

            var client = new Client { Id = 1, FullName = "Acme Corp" };
            var svc = new Service { Id = 1, Name = "Electrical", HourlyRate = 60, IsActive = true };
            context.Clients.Add(client);
            context.Services.Add(svc);

            var weekStart = new DateTime(2026, 9, 7);

            context.Bookings.AddRange(
                new Booking
                {
                    Id = 1,
                    ClientId = client.Id,
                    VisitDate = weekStart.AddDays(3),
                    BookingDetails = new List<BookingDetail> { new BookingDetail { ServiceId = svc.Id } }
                },
                new Booking
                {
                    Id = 2,
                    ClientId = client.Id,
                    VisitDate = weekStart.AddDays(1),
                    BookingDetails = new List<BookingDetail> { new BookingDetail { ServiceId = svc.Id } }
                }
            );
            await context.SaveChangesAsync();

            var scheduleService = new ScheduleService(context);
            var result = await scheduleService.GetWeeklyScheduleAsync(weekStart);

            Assert.Equal(2, result.Count);
            Assert.True(result[0].VisitDate < result[1].VisitDate);
        }

        [Fact]
        public async Task GetWeeklyScheduleAsync_IncludesClientAndServiceNavigationProperties()
        {
            using var context = CreateContext(nameof(GetWeeklyScheduleAsync_IncludesClientAndServiceNavigationProperties));

            var client = new Client { Id = 1, FullName = "Acme Corp" };
            var svc = new Service { Id = 1, Name = "Roofing", HourlyRate = 70, IsActive = true };
            context.Clients.Add(client);
            context.Services.Add(svc);

            var weekStart = new DateTime(2026, 9, 7);
            context.Bookings.Add(new Booking
            {
                Id = 1,
                ClientId = client.Id,
                VisitDate = weekStart.AddDays(2),
                BookingDetails = new List<BookingDetail> { new BookingDetail { ServiceId = svc.Id } }
            });
            await context.SaveChangesAsync();

            var scheduleService = new ScheduleService(context);
            var result = await scheduleService.GetWeeklyScheduleAsync(weekStart);

            Assert.Single(result);
            Assert.NotNull(result[0].Client);
            Assert.Equal("Acme Corp", result[0].Client!.FullName);
            Assert.NotEmpty(result[0].BookingDetails);
            Assert.NotNull(result[0].BookingDetails.First().Service);
        }

        [Fact]
        public async Task GetWeeklyScheduleAsync_ReturnsEmptyList_WhenNoBookingsInRange()
        {
            using var context = CreateContext(nameof(GetWeeklyScheduleAsync_ReturnsEmptyList_WhenNoBookingsInRange));
            var scheduleService = new ScheduleService(context);

            var result = await scheduleService.GetWeeklyScheduleAsync(new DateTime(2026, 9, 7));

            Assert.Empty(result);
        }
    }
}