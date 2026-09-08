using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ConstructionServices.Data;

namespace ConstructionServicesManagementSystem.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly AppDbContext _context;

        public ScheduleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetWeeklyScheduleAsync(DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);

            return await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Service)
                .Where(b => b.VisitDate >= weekStart &&
                            b.VisitDate < weekEnd)
                .OrderBy(b => b.VisitDate)
                .ToListAsync();
        }
    }
}