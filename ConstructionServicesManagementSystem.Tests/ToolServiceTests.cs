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
    public class ToolServiceTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllToolsAsync_ReturnsToolsOrderedByName_WithServiceIncluded()
        {
            using var context = CreateContext(nameof(GetAllToolsAsync_ReturnsToolsOrderedByName_WithServiceIncluded));
            var svc = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            context.Services.Add(svc);
            context.Tools.AddRange(
                new Tool { Id = 1, Name = "Wrench", ServiceId = svc.Id, Quantity = 3, IsAvailable = true },
                new Tool { Id = 2, Name = "Drill", ServiceId = svc.Id, Quantity = 5, IsAvailable = true }
            );
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var result = await toolService.GetAllToolsAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("Drill", result[0].Name);
            Assert.Equal("Wrench", result[1].Name);
            Assert.NotNull(result[0].Service);
        }

        [Fact]
        public async Task GetAllToolsAsync_Paged_FiltersBySearchTerm()
        {
            using var context = CreateContext(nameof(GetAllToolsAsync_Paged_FiltersBySearchTerm));
            var svc = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            context.Services.Add(svc);
            context.Tools.AddRange(
                new Tool { Id = 1, Name = "Wrench", ServiceId = svc.Id, Quantity = 3, IsAvailable = true },
                new Tool { Id = 2, Name = "Drill", ServiceId = svc.Id, Quantity = 5, IsAvailable = true },
                new Tool { Id = 3, Name = "Wire Cutter", ServiceId = svc.Id, Quantity = 2, IsAvailable = true }
            );
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var result = await toolService.GetAllToolsAsync(pageNumber: 1, pageSize: 10, search: "Wi");

            Assert.Single(result);
            Assert.Equal("Wire Cutter", result[0].Name);
        }

        [Fact]
        public async Task GetAllToolsAsync_Paged_FiltersByServiceId()
        {
            using var context = CreateContext(nameof(GetAllToolsAsync_Paged_FiltersByServiceId));
            var svc1 = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            var svc2 = new Service { Id = 2, Name = "Electrical", HourlyRate = 60, IsActive = true };
            context.Services.AddRange(svc1, svc2);
            context.Tools.AddRange(
                new Tool { Id = 1, Name = "Wrench", ServiceId = svc1.Id, Quantity = 3, IsAvailable = true },
                new Tool { Id = 2, Name = "Voltage Tester", ServiceId = svc2.Id, Quantity = 4, IsAvailable = true }
            );
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var result = await toolService.GetAllToolsAsync(pageNumber: 1, pageSize: 10, serviceId: svc2.Id);

            Assert.Single(result);
            Assert.Equal("Voltage Tester", result[0].Name);
        }

        [Fact]
        public async Task GetAllToolsAsync_Paged_RespectsPagination()
        {
            using var context = CreateContext(nameof(GetAllToolsAsync_Paged_RespectsPagination));
            var svc = new Service { Id = 1, Name = "General", HourlyRate = 40, IsActive = true };
            context.Services.Add(svc);
            for (int i = 1; i <= 5; i++)
            {
                context.Tools.Add(new Tool { Id = i, Name = $"Tool{i:D2}", ServiceId = svc.Id, Quantity = i, IsAvailable = true });
            }
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var page1 = await toolService.GetAllToolsAsync(pageNumber: 1, pageSize: 2);
            var page2 = await toolService.GetAllToolsAsync(pageNumber: 2, pageSize: 2);

            Assert.Equal(2, page1.Count);
            Assert.Equal(2, page2.Count);
            Assert.NotEqual(page1[0].Id, page2[0].Id);
        }

        [Fact]
        public async Task GetToolByIdAsync_ReturnsCorrectTool_WhenExists()
        {
            using var context = CreateContext(nameof(GetToolByIdAsync_ReturnsCorrectTool_WhenExists));
            var svc = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            context.Services.Add(svc);
            context.Tools.Add(new Tool { Id = 1, Name = "Pipe Cutter", ServiceId = svc.Id, Quantity = 2, IsAvailable = true });
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var result = await toolService.GetToolByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Pipe Cutter", result!.Name);
            Assert.NotNull(result.Service);
        }

        [Fact]
        public async Task GetToolByIdAsync_ReturnsNull_WhenNotFound()
        {
            using var context = CreateContext(nameof(GetToolByIdAsync_ReturnsNull_WhenNotFound));
            var toolService = new ToolService(context);

            var result = await toolService.GetToolByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateToolAsync_AddsToolToDatabase()
        {
            using var context = CreateContext(nameof(CreateToolAsync_AddsToolToDatabase));
            var svc = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            context.Services.Add(svc);
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var newTool = new Tool { Name = "Hacksaw", ServiceId = svc.Id, Quantity = 1, IsAvailable = true };

            var result = await toolService.CreateToolAsync(newTool);

            Assert.NotEqual(0, result.Id);
            Assert.Equal(1, await context.Tools.CountAsync());
        }

        [Fact]
        public async Task UpdateToolAsync_UpdatesFields_WhenToolExists()
        {
            using var context = CreateContext(nameof(UpdateToolAsync_UpdatesFields_WhenToolExists));
            var svc1 = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            var svc2 = new Service { Id = 2, Name = "Electrical", HourlyRate = 60, IsActive = true };
            context.Services.AddRange(svc1, svc2);
            context.Tools.Add(new Tool { Id = 1, Name = "Wrench", ServiceId = svc1.Id, Quantity = 3, Description = "Old", IsAvailable = true });
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var updated = new Tool
            {
                Id = 1,
                Name = "Adjustable Wrench",
                ServiceId = svc2.Id,
                Quantity = 10,
                Description = "New",
                IsAvailable = false
            };
            var success = await toolService.UpdateToolAsync(updated);

            Assert.True(success);
            var persisted = await context.Tools.FindAsync(1);
            Assert.Equal("Adjustable Wrench", persisted!.Name);
            Assert.Equal(svc2.Id, persisted.ServiceId);
            Assert.Equal(10, persisted.Quantity);
            Assert.Equal("New", persisted.Description);
            Assert.False(persisted.IsAvailable);
        }

        [Fact]
        public async Task UpdateToolAsync_ReturnsFalse_WhenToolDoesNotExist()
        {
            using var context = CreateContext(nameof(UpdateToolAsync_ReturnsFalse_WhenToolDoesNotExist));
            var toolService = new ToolService(context);
            var nonExistent = new Tool { Id = 999, Name = "Ghost Tool", Quantity = 1, IsAvailable = true };

            var success = await toolService.UpdateToolAsync(nonExistent);

            Assert.False(success);
        }

        [Fact]
        public async Task DeleteToolAsync_RemovesTool_WhenExists()
        {
            using var context = CreateContext(nameof(DeleteToolAsync_RemovesTool_WhenExists));
            var svc = new Service { Id = 1, Name = "Plumbing", HourlyRate = 50, IsActive = true };
            context.Services.Add(svc);
            context.Tools.Add(new Tool { Id = 1, Name = "Wrench", ServiceId = svc.Id, Quantity = 3, IsAvailable = true });
            await context.SaveChangesAsync();

            var toolService = new ToolService(context);
            var success = await toolService.DeleteToolAsync(1);

            Assert.True(success);
            Assert.Equal(0, await context.Tools.CountAsync());
        }

        [Fact]
        public async Task DeleteToolAsync_ReturnsFalse_WhenToolDoesNotExist()
        {
            using var context = CreateContext(nameof(DeleteToolAsync_ReturnsFalse_WhenToolDoesNotExist));
            var toolService = new ToolService(context);

            var success = await toolService.DeleteToolAsync(999);

            Assert.False(success);
        }
    }
}