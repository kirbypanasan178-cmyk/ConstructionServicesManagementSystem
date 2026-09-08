using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class ToolController : Controller
    {
        private readonly IToolService _toolService;
        private readonly IHourlyRateService _hourlyRateService;

        public ToolController(
            IToolService toolService,
            IHourlyRateService hourlyRateService)
        {
            _toolService = toolService;
            _hourlyRateService = hourlyRateService;
        }

        // GET: Tool
        public async Task<IActionResult> Index()
        {
            var tools = await _toolService.GetAllToolsAsync();

            return View(tools);
        }

        // GET: Tool/Create
        public async Task<IActionResult> Create()
        {
            await LoadServices();

            return View();
        }

        // POST: Tool/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tool tool)
        {
            if (!ModelState.IsValid)
            {
                await LoadServices(tool.ServiceId);
                return View(tool);
            }

            await _toolService.CreateToolAsync(tool);

            return RedirectToAction(nameof(Index));
        }

        // GET: Tool/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var tool = await _toolService.GetToolByIdAsync(id);

            if (tool == null)
            {
                return NotFound();
            }

            await LoadServices(tool.ServiceId);

            return View(tool);
        }

        // POST: Tool/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tool tool)
        {
            if (id != tool.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadServices(tool.ServiceId);
                return View(tool);
            }

            var updated = await _toolService.UpdateToolAsync(tool);

            if (!updated)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Tool/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var tool = await _toolService.GetToolByIdAsync(id);

            if (tool == null)
            {
                return NotFound();
            }

            return View(tool);
        }

        // POST: Tool/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _toolService.DeleteToolAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        // Load services for dropdown
        private async Task LoadServices(int? selectedServiceId = null)
        {
            var services = await _hourlyRateService.GetAllAsync();

            ViewBag.Services = new SelectList(
                services,
                "Id",
                "Name",
                selectedServiceId
            );
        }
    }
}