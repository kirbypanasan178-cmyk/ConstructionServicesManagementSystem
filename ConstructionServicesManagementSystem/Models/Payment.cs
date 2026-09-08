using ConstructionServicesManagementSystem.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConstructionServicesManagementSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int BillingId { get; set; }

        public Billing Billing { get; set; } = null!;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public PaymentMethod PaymentMethod { get; set; }

        public string ReferenceNumber { get; set; } = string.Empty;
    }
}
