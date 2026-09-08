using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<List<Billing>> GetPendingBillingsAsync(); // keep for non-paged usage
        Task<List<Billing>> GetPendingBillingsAsync(int pageNumber, int pageSize, string? search = null);

        Task<Billing?> GetBillingByIdAsync(int billingId);

        Task<Payment?> ProcessPaymentAsync(
            int billingId,
            decimal amount,
            PaymentMethod paymentMethod);
    }
}