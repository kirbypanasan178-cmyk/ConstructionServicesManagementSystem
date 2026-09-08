using ConstructionServicesManagementSystem.Enums;

namespace ConstructionServicesManagementSystem.Models
{
    public class Billing
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public Booking Booking { get; set; } = null!;

        public string BillingNumber { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal Balance { get; set; }

        public BillingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}
