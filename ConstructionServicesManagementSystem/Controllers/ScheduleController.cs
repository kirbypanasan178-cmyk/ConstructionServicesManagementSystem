using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        public async Task<IActionResult> Weekly(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;

            // Find Monday of the selected week
            int daysFromMonday =
                ((int)selectedDate.DayOfWeek + 6) % 7;

            var weekStart = selectedDate.Date.AddDays(-daysFromMonday);

            var bookings = await _scheduleService
                .GetWeeklyScheduleAsync(weekStart);

            ViewBag.WeekStart = weekStart;
            ViewBag.WeekEnd = weekStart.AddDays(6);

            return View(bookings);
        }
    }
}