using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IScheduleService
    {
        Task<List<Booking>> GetWeeklyScheduleAsync(DateTime weekStart);
    }
}