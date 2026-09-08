using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<List<Billing>> GetPendingBillingsAsync();

        Task<Billing?> GetBillingByIdAsync(int billingId);

        Task<Payment?> ProcessPaymentAsync(
            int billingId,
            decimal amount,
            PaymentMethod paymentMethod);
    }
}