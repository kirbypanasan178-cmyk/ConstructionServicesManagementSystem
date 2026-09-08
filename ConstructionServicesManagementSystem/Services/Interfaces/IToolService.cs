using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IToolService
    {
        Task<List<Tool>> GetAllToolsAsync(); // for dropdowns
        Task<List<Tool>> GetAllToolsAsync(int pageNumber, int pageSize, string? search = null, int? serviceId = null); Task<Tool?> GetToolByIdAsync(int id);
        Task<Tool> CreateToolAsync(Tool tool);
        Task<bool> UpdateToolAsync(Tool tool);
        Task<bool> DeleteToolAsync(int id);
    }
}