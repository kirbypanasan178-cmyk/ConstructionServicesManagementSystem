using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IToolService
    {
        Task<List<Tool>> GetAllToolsAsync();

        Task<Tool?> GetToolByIdAsync(int id);

        Task<Tool> CreateToolAsync(Tool tool);

        Task<bool> UpdateToolAsync(Tool tool);

        Task<bool> DeleteToolAsync(int id);
    }
}