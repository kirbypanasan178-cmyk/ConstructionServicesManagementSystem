using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IClientService _clientService;
        private readonly IHourlyRateService _hourlyRateService;

        public BookingController(
            IBookingService bookingService,
            IClientService clientService,
            IHourlyRateService hourlyRateService)
        {
            _bookingService = bookingService;
            _clientService = clientService;
            _hourlyRateService = hourlyRateService;
        }

        // GET: Booking
        public async Task<IActionResult> Index(int page = 1, string? search = null, BillingStatus? status = null)
        {
            int pageSize = 10;

            var bookings = await _bookingService.GetAllAsync(page, pageSize, search, status);

            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = bookings.Count == pageSize;
            ViewBag.HasPreviousPage = page > 1;

            return View(bookings);
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Booking/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = await _clientService.GetAllClientsAsync();
            ViewBag.Services = await _hourlyRateService.GetAllAsync();

            return View();
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int clientId,
            DateTime visitDate,
            List<BookingDetail> bookingDetails)
        {
            try
            {
                await _bookingService.CreateAsync(
                    clientId,
                    visitDate,
                    bookingDetails);

                TempData["SuccessMessage"] =
                    "Booking created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                ViewBag.Clients =
                    await _clientService.GetAllClientsAsync();

                ViewBag.Services =
                    await _hourlyRateService.GetAllAsync();

                return View();
            }
        }
    }
}