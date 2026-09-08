using ConstructionServicesManagementSystem.Models;

namespace ConstructionServicesManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClients { get; set; }

        public int UpcomingBookings { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal PendingAmount { get; set; }

        public List<Booking> UpcomingSchedule { get; set; } = new();

        public List<Payment> RecentPayments { get; set; } = new();
    }
}