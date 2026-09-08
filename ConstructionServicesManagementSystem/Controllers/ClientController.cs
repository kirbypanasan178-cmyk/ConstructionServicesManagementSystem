using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class ClientController : Controller
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        // GET: /Client
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            int pageSize = 10;

            var clients = await _clientService.GetAllClientsAsync(page, pageSize, search);

            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = clients.Count == pageSize;
            ViewBag.HasPreviousPage = page > 1;

            return View(clients);
        }

        // GET: /Client/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: /Client/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Client/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            if (!ModelState.IsValid)
            {
                return View(client);
            }

            await _clientService.CreateClientAsync(client);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Client/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: /Client/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(client);
            }

            var updated = await _clientService.UpdateClientAsync(client);

            if (!updated)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}