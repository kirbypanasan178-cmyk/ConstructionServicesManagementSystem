using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await _dashboardService.GetDashboardAsync();

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}