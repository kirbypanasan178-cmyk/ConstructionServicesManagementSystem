using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IHourlyRateService
    {
        Task<List<Service>> GetAllAsync();

        Task<Service?> GetByIdAsync(int id);

        Task<Service> CreateAsync(Service service);

        Task<bool> UpdateAsync(Service service);

        Task<bool> DeleteAsync(int id);
    }
}