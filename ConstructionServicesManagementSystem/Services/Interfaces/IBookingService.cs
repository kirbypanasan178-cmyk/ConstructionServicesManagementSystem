using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IBookingService
    {
        Task<List<Booking>> GetAllAsync();

        Task<Booking?> GetByIdAsync(int id);

        Task<bool> IsDateBookedAsync(DateTime visitDate);

        Task<Booking> CreateAsync(
            int clientId,
            DateTime visitDate,
            List<BookingDetail> bookingDetails);
    }
}