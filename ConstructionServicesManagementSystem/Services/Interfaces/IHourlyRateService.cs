using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IHourlyRateService
    {
        Task<List<Service>> GetAllAsync(); // for dropdowns
        Task<List<Service>> GetAllAsync(int pageNumber, int pageSize, string? search = null);

        Task<Service?> GetByIdAsync(int id);

        Task<Service> CreateAsync(Service service);

        Task<bool> UpdateAsync(Service service);

        Task<bool> DeleteAsync(int id);
    }
}