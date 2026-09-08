using ConstructionServicesManagementSystem.ViewModels;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ConstructionServices.Data;

namespace ConstructionServicesManagementSystem.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var today = DateTime.Today;

            var totalClients = await _context.Clients
                .CountAsync();

            var upcomingSchedule = await _context.Bookings
                .Include(b => b.Client)
                .Where(b => b.VisitDate >= today)
                .OrderBy(b => b.VisitDate)
                .Take(5)
                .ToListAsync();

            var upcomingBookings = await _context.Bookings
                .CountAsync(b => b.VisitDate >= today);

            var totalRevenue = await _context.Payments
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

           var recentPayments = await _context.Payments
                .Include(p => p.Billing)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();     

            var pendingAmount = await _context.Billings
                .Where(b => b.Status != Enums.BillingStatus.Paid)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            return new DashboardViewModel
            {
                TotalClients = totalClients,
                UpcomingBookings = upcomingBookings,
                TotalRevenue = totalRevenue,
                PendingAmount = pendingAmount,
                UpcomingSchedule = upcomingSchedule,
                RecentPayments = recentPayments
            };
        }
    }
}