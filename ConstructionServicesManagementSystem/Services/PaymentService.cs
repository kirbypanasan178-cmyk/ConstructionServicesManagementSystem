using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServicesManagementSystem.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Billing>> GetPendingBillingsAsync()
        {
            return await _context.Billings
                .Include(b => b.Booking)
                .Where(b => b.Balance > 0)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Billing?> GetBillingByIdAsync(int billingId)
        {
            return await _context.Billings
                .Include(b => b.Booking)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == billingId);
        }

        public async Task<Payment?> ProcessPaymentAsync(
            int billingId,
            decimal amount,
            PaymentMethod paymentMethod)
        {
            var billing = await _context.Billings
                .FirstOrDefaultAsync(b => b.Id == billingId);

            if (billing == null)
                return null;

            if (amount <= 0)
                return null;

            if (amount > billing.Balance)
                return null;

            // Generate unique payment reference number
            var referenceNumber =
                $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var payment = new Payment
            {
                BillingId = billingId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = paymentMethod,
                ReferenceNumber = referenceNumber
            };

            billing.AmountPaid += amount;
            billing.Balance = billing.TotalAmount - billing.AmountPaid;

            if (billing.Balance <= 0)
            {
                billing.Balance = 0;
                billing.Status = BillingStatus.Paid;
            }
            else
            {
                billing.Status = BillingStatus.PartiallyPaid;
            }

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            return payment;
        }
    }
}