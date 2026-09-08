using System.ComponentModel.DataAnnotations;

namespace ConstructionServicesManagementSystem.Models
{
    public class BookingDetail
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public Booking Booking { get; set; } = null!;

        public int ServiceId { get; set; }

        public Service Service { get; set; } = null!;

        [Range(0.1, double.MaxValue)]
        public decimal HoursRendered { get; set; }

        public decimal HourlyRate { get; set; }

        public decimal Amount { get; set; }
    }
}
