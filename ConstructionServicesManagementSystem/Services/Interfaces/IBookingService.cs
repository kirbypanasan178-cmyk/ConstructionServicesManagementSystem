using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IBookingService
    {
        Task<List<Booking>> GetAllAsync(); // keep for any non-paged usage
        Task<List<Booking>> GetAllAsync(int pageNumber, int pageSize, string? search = null, BillingStatus? billingStatus = null);

        Task<Booking?> GetByIdAsync(int id);

        Task<bool> IsDateBookedAsync(DateTime visitDate);

        Task<Booking> CreateAsync(
            int clientId,
            DateTime visitDate,
            List<BookingDetail> bookingDetails);
    }
}