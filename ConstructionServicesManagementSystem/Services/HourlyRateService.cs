using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServicesManagementSystem.Services
{
    public class HourlyRateService : IHourlyRateService
    {
        private readonly AppDbContext _context;

        public HourlyRateService(AppDbContext context)
        {
            _context = context;
        }

        // Get all services
        public async Task<List<Service>> GetAllAsync()
        {
            return await _context.Services
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        // Get one service
        public async Task<Service?> GetByIdAsync(int id)
        {
            return await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        // Add a new service
        public async Task<Service> CreateAsync(Service service)
        {
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return service;
        }

        // Edit an existing service
        public async Task<bool> UpdateAsync(Service service)
        {
            var existingService = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == service.Id);

            if (existingService == null)
            {
                return false;
            }

            existingService.Name = service.Name;
            existingService.HourlyRate = service.HourlyRate;
            existingService.IsActive = service.IsActive;

            await _context.SaveChangesAsync();

            return true;
        }

        // Delete a service
        public async Task<bool> DeleteAsync(int id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
            {
                return false;
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}