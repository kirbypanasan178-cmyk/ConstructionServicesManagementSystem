using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class HourlyRateController : Controller
    {
        private readonly IHourlyRateService _hourlyRateService;

        public HourlyRateController(IHourlyRateService hourlyRateService)
        {
            _hourlyRateService = hourlyRateService;
        }

        // Display all services and hourly rates
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            int pageSize = 10;

            var services = await _hourlyRateService.GetAllAsync(page, pageSize, search);

            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = services.Count == pageSize;
            ViewBag.HasPreviousPage = page > 1;

            return View(services);
        }

        // Add service - GET
        public IActionResult Create()
        {
            return View();
        }

        // Add service - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (!ModelState.IsValid)
            {
                return View(service);
            }

            await _hourlyRateService.CreateAsync(service);

            TempData["SuccessMessage"] =
                "Service and hourly rate added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Edit service - GET
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _hourlyRateService.GetByIdAsync(id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // Edit service - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(service);
            }

            var updated = await _hourlyRateService.UpdateAsync(service);

            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Service and hourly rate updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Delete service - GET
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _hourlyRateService.GetByIdAsync(id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // Delete service - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _hourlyRateService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Service deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}