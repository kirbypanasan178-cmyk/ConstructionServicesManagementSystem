using System.ComponentModel.DataAnnotations;

namespace ConstructionServicesManagementSystem.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public Client Client { get; set; } = null!;

        [Required]
        public DateTime VisitDate { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BookingDetail> BookingDetails { get; set; }
            = new List<BookingDetail>();

        public Billing? Billing { get; set; }
    }
}
