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
    // Uses EF Core's InMemory provider so each test gets an isolated, fast
    // "database" without needing a real SQL Server/Postgres instance.
    public class HourlyRateServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsServicesOrderedByName()
        {
            using var context = CreateContext(nameof(GetAllAsync_ReturnsServicesOrderedByName));
            context.Services.AddRange(
                new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true },
                new Service { Id = 2, Name = "Electrical", HourlyRate = 60, IsActive = true }
            );
            await context.SaveChangesAsync();

            var service = new HourlyRateService(context);
            var result = await service.GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("Electrical", result[0].Name); // alphabetical order
            Assert.Equal("Plumbing", result[1].Name);
        }

        [Fact]
        public async Task GetAllAsync_Paged_FiltersBySearchTerm()
        {
            using var context = CreateContext(nameof(GetAllAsync_Paged_FiltersBySearchTerm));
            context.Services.AddRange(
                new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true },
                new Service { Id = 2, Name = "Electrical", HourlyRate = 60, IsActive = true },
                new Service { Id = 3, Name = "Plastering", HourlyRate = 45, IsActive = true }
            );
            await context.SaveChangesAsync();

            var service = new HourlyRateService(context);
            var result = await service.GetAllAsync(pageNumber: 1, pageSize: 10, search: "Pl");

            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Contains("Pl", s.Name));
        }

        [Fact]
        public async Task GetAllAsync_Paged_RespectsPageNumberAndSize()
        {
            using var context = CreateContext(nameof(GetAllAsync_Paged_RespectsPageNumberAndSize));
            for (int i = 1; i <= 5; i++)
            {
                context.Services.Add(new Service { Id = i, Name = $"Service{i:D2}", HourlyRate = 10 * i, IsActive = true });
            }
            await context.SaveChangesAsync();

            var service = new HourlyRateService(context);
            var page1 = await service.GetAllAsync(pageNumber: 1, pageSize: 2);
            var page2 = await service.GetAllAsync(pageNumber: 2, pageSize: 2);

            Assert.Equal(2, page1.Count);
            Assert.Equal(2, page2.Count);
            Assert.NotEqual(page1[0].Id, page2[0].Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectService_WhenExists()
        {
            using var context = CreateContext(nameof(GetByIdAsync_ReturnsCorrectService_WhenExists));
            context.Services.Add(new Service { Id = 1, Name = "Roofing", HourlyRate = 75, IsActive = true });
            await context.SaveChangesAsync();

            var service = new HourlyRateService(context);
            var result = await service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Roofing", result!.Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            using var context = CreateContext(nameof(GetByIdAsync_ReturnsNull_WhenNotFound));
            var service = new HourlyRateService(context);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_AddsServiceToDatabase()
        {
            using var context = CreateContext(nameof(CreateAsync_AddsServiceToDatabase));
            var service = new HourlyRateService(context);
            var newService = new Service { Name = "Carpentry", HourlyRate = 55, IsActive = true };

            var result = await service.CreateAsync(newService);

            Assert.NotEqual(0, result.Id);
            Assert.Equal(1, await context.Services.CountAsync());
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields_WhenServiceExists()
        {
            using var context = CreateContext(nameof(UpdateAsync_UpdatesFields_WhenServiceExists));
            context.Services.Add(new Service { Id = 1, Name = "Painting", HourlyRate = 40, IsActive = true });
            await context.SaveChangesAsync();

            var service = new HourlyRateService(context);
            var updated = new Service { Id = 1, Name = "Painting (Updated)", HourlyRate = 48, IsActive = false };
            var success = await service.UpdateAsync(updated);

            Assert.True(success);
            var persisted = await context.Services.FindAsync(1);
            Assert.Equal("Painting (Updated)", persisted!.Name);
            Assert.Equal(48, persisted.HourlyRate);
            Assert.False(persisted.IsActive);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenServiceDoesNotExist()
        {
            using var context = CreateContext(nameof(UpdateAsync_ReturnsFalse_WhenServiceDoesNotExist));
            var service = new HourlyRateService(context);
            var nonExistent = new Service { Id = 999, Name = "Ghost", HourlyRate = 10, IsActive = true };

            var success = await service.UpdateAsync(nonExistent);

            Assert.False(success);
        }

        [Fact]
        public async Task DeleteAsync_RemovesService_WhenExists()
        {
            using var context = CreateContext(nameof(DeleteAsync_RemovesService_WhenExists));
            context.Services.Add(new Service { Id = 1, Name = "Demolition", HourlyRate = 65, IsActive = true });
            await context.SaveChangesAsync();

            var service = new HourlyRateService(context);
            var success = await service.DeleteAsync(1);

            Assert.True(success);
            Assert.Equal(0, await context.Services.CountAsync());
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenServiceDoesNotExist()
        {
            using var context = CreateContext(nameof(DeleteAsync_ReturnsFalse_WhenServiceDoesNotExist));
            var service = new HourlyRateService(context);

            var success = await service.DeleteAsync(999);

            Assert.False(success);
        }
    }
}