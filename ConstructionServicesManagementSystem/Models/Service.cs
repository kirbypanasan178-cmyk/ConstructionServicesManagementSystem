using System.ComponentModel.DataAnnotations;

namespace ConstructionServicesManagementSystem.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
