using ConstructionServices.Data;
using ConstructionServicesManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServicesManagementSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> BillingStatement(int? clientId, int page = 1, string? search = null)
        {
            int pageSize = 10;

            var clientQuery = _context.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                clientQuery = clientQuery.Where(c =>
                    c.FullName.Contains(search) ||
                    c.Email.Contains(search) ||
                    c.PhoneNumber.Contains(search));
            }

            var clients = await clientQuery
                .OrderBy(c => c.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Clients = clients;
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.HasNextPage = clients.Count == pageSize;
            ViewBag.HasPreviousPage = page > 1;

            // No customer selected
            if (clientId == null)
            {
                return View();
            }

            // Find customer
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null)
            {
                return NotFound();
            }

            // Get billing records for selected customer
            var billings = await _context.Billings
                .Include(b => b.Booking)
                    .ThenInclude(b => b.Client)
                .Include(b => b.Booking)
                    .ThenInclude(b => b.BookingDetails)
                        .ThenInclude(d => d.Service)
                .Where(b => b.Booking.ClientId == clientId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var statement = new BillingStatementViewModel
            {
                ClientId = client.Id,
                ClientName = client.FullName
            };

            foreach (var billing in billings)
            {
                var paid = await _context.Payments
                    .Where(p => p.BillingId == billing.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var balance = billing.TotalAmount - paid;

                var serviceName = string.Join(
                    ", ",
                    billing.Booking.BookingDetails
                        .Select(d => d.Service.Name)
                );

                statement.Items.Add(new BillingStatementItemViewModel
                {
                    BillingId = billing.Id,
                    Date = billing.CreatedAt,
                    ServiceName = serviceName,
                    Amount = billing.TotalAmount,
                    Paid = paid,
                    Balance = balance
                });
            }

            statement.TotalBilling = statement.Items.Sum(x => x.Amount);
            statement.TotalPaid = statement.Items.Sum(x => x.Paid);
            statement.Balance = statement.Items.Sum(x => x.Balance);

            return View(statement);
        }
    }
}