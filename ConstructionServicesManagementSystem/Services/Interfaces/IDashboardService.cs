using ConstructionServicesManagementSystem.ViewModels;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync();
    }
}