using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServicesManagementSystem.Services
{
    public class ClientService : IClientService
    {
        private readonly AppDbContext _context;

        public ClientService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _context.Clients
                .OrderBy(c => c.FullName)
                .ToListAsync();
        }

        public async Task<List<Client>> GetAllClientsAsync(int pageNumber, int pageSize, string? search = null)
        {
            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || c.PhoneNumber.Contains(search));
            }

            return await query
                .OrderBy(c => c.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Client> CreateClientAsync(Client client)
        {
            client.CreatedAt = DateTime.UtcNow;

            _context.Clients.Add(client);

            await _context.SaveChangesAsync();

            return client;
        }

        public async Task<bool> UpdateClientAsync(Client client)
        {
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == client.Id);

            if (existingClient == null)
            {
                return false;
            }

            existingClient.FullName = client.FullName;
            existingClient.Address = client.Address;
            existingClient.PhoneNumber = client.PhoneNumber;
            existingClient.Email = client.Email;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}