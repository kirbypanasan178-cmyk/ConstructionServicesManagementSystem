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
    public class ClientServiceTests
    {
        // Each test gets its own isolated InMemory database so tests don't
        // interfere with each other.
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static Client MakeClient(
            string fullName = "John Doe",
            string email = "john@example.com",
            string phone = "0912-345-6789",
            string address = "Davao City")
        {
            return new Client
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                Address = address
            };
        }

        // ---------- GetClientByIdAsync ----------

        [Fact]
        public async Task GetClientByIdAsync_ReturnsClient_WhenClientExists()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            var result = await service.GetClientByIdAsync(client.Id);

            Assert.NotNull(result);
            Assert.Equal(client.Id, result!.Id);
            Assert.Equal("John Doe", result.FullName);
        }

        [Fact]
        public async Task GetClientByIdAsync_ReturnsNull_WhenClientDoesNotExist()
        {
            using var context = CreateContext();
            var service = new ClientService(context);

            var result = await service.GetClientByIdAsync(999);

            Assert.Null(result);
        }

        // ---------- GetAllClientsAsync (no paging) ----------

        [Fact]
        public async Task GetAllClientsAsync_ReturnsClientsOrderedByFullName()
        {
            using var context = CreateContext();
            context.Clients.AddRange(
                MakeClient(fullName: "Zack Reyes"),
                MakeClient(fullName: "Anna Cruz"),
                MakeClient(fullName: "Mike Santos")
            );
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            var result = await service.GetAllClientsAsync();

            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { "Anna Cruz", "Mike Santos", "Zack Reyes" },
                result.Select(c => c.FullName).ToArray());
        }

        [Fact]
        public async Task GetAllClientsAsync_ReturnsEmptyList_WhenNoClientsExist()
        {
            using var context = CreateContext();
            var service = new ClientService(context);

            var result = await service.GetAllClientsAsync();

            Assert.Empty(result);
        }

        // ---------- GetAllClientsAsync (paged + search) ----------

        [Fact]
        public async Task GetAllClientsAsync_Paged_ReturnsCorrectPage()
        {
            using var context = CreateContext();
            context.Clients.AddRange(
                MakeClient(fullName: "Anna Cruz"),
                MakeClient(fullName: "Bob Diaz"),
                MakeClient(fullName: "Carl Evans"),
                MakeClient(fullName: "Dana Flores"),
                MakeClient(fullName: "Eli Garcia")
            );
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            // Page size 2, page 2 -> should be items 3 and 4 alphabetically
            var result = await service.GetAllClientsAsync(pageNumber: 2, pageSize: 2);

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Carl Evans", "Dana Flores" },
                result.Select(c => c.FullName).ToArray());
        }

        [Fact]
        public async Task GetAllClientsAsync_Paged_FiltersBySearch_MatchingFullName()
        {
            using var context = CreateContext();
            context.Clients.AddRange(
                MakeClient(fullName: "Anna Cruz", email: "anna@example.com"),
                MakeClient(fullName: "Bob Diaz", email: "bob@example.com")
            );
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            var result = await service.GetAllClientsAsync(1, 10, search: "Anna");

            Assert.Single(result);
            Assert.Equal("Anna Cruz", result[0].FullName);
        }

        [Fact]
        public async Task GetAllClientsAsync_Paged_FiltersBySearch_MatchingEmailOrPhone()
        {
            using var context = CreateContext();
            context.Clients.AddRange(
                MakeClient(fullName: "Anna Cruz", email: "anna@construction.com", phone: "0917-000-1111"),
                MakeClient(fullName: "Bob Diaz", email: "bob@example.com", phone: "0918-222-3333")
            );
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            var byEmail = await service.GetAllClientsAsync(1, 10, search: "construction.com");
            var byPhone = await service.GetAllClientsAsync(1, 10, search: "0918-222-3333");

            Assert.Single(byEmail);
            Assert.Equal("Anna Cruz", byEmail[0].FullName);

            Assert.Single(byPhone);
            Assert.Equal("Bob Diaz", byPhone[0].FullName);
        }

        [Fact]
        public async Task GetAllClientsAsync_Paged_ReturnsEmptyList_WhenSearchMatchesNothing()
        {
            using var context = CreateContext();
            context.Clients.Add(MakeClient(fullName: "Anna Cruz"));
            await context.SaveChangesAsync();

            var service = new ClientService(context);

            var result = await service.GetAllClientsAsync(1, 10, search: "no-such-client");

            Assert.Empty(result);
        }

        // ---------- CreateClientAsync ----------

        [Fact]
        public async Task CreateClientAsync_AddsClient_AndSetsCreatedAt()
        {
            using var context = CreateContext();
            var service = new ClientService(context);
            var client = MakeClient();
            var beforeCreate = DateTime.UtcNow;

            var result = await service.CreateClientAsync(client);

            var afterCreate = DateTime.UtcNow;

            Assert.NotEqual(0, result.Id);
            Assert.True(result.CreatedAt >= beforeCreate && result.CreatedAt <= afterCreate);

            var stored = await context.Clients.FindAsync(result.Id);
            Assert.NotNull(stored);
            Assert.Equal("John Doe", stored!.FullName);
        }

        // ---------- UpdateClientAsync ----------

        [Fact]
        public async Task UpdateClientAsync_UpdatesFields_AndReturnsTrue_WhenClientExists()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            // Detach so this behaves like an update coming from a different request/DTO.
            context.Entry(client).State = EntityState.Detached;

            var service = new ClientService(context);

            var updated = new Client
            {
                Id = client.Id,
                FullName = "John Updated",
                Address = "New Address",
                PhoneNumber = "0999-999-9999",
                Email = "updated@example.com"
            };

            var success = await service.UpdateClientAsync(updated);

            Assert.True(success);

            var stored = await context.Clients.FindAsync(client.Id);
            Assert.NotNull(stored);
            Assert.Equal("John Updated", stored!.FullName);
            Assert.Equal("New Address", stored.Address);
            Assert.Equal("0999-999-9999", stored.PhoneNumber);
            Assert.Equal("updated@example.com", stored.Email);
        }

        [Fact]
        public async Task UpdateClientAsync_ReturnsFalse_WhenClientDoesNotExist()
        {
            using var context = CreateContext();
            var service = new ClientService(context);

            var updated = new Client
            {
                Id = 12345,
                FullName = "Nobody",
                Address = "Nowhere",
                PhoneNumber = "0000-000-0000",
                Email = "nobody@example.com"
            };

            var success = await service.UpdateClientAsync(updated);

            Assert.False(success);
        }

        [Fact]
        public async Task UpdateClientAsync_DoesNotChangeCreatedAt()
        {
            using var context = CreateContext();
            var client = MakeClient();
            context.Clients.Add(client);
            await context.SaveChangesAsync();
            var originalCreatedAt = client.CreatedAt;

            context.Entry(client).State = EntityState.Detached;

            var service = new ClientService(context);
            var updated = new Client
            {
                Id = client.Id,
                FullName = "John Updated",
                Address = client.Address,
                PhoneNumber = client.PhoneNumber,
                Email = client.Email
            };

            await service.UpdateClientAsync(updated);

            var stored = await context.Clients.FindAsync(client.Id);
            Assert.Equal(originalCreatedAt, stored!.CreatedAt);
        }
    }
}