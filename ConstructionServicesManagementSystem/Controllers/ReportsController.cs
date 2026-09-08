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

        public async Task<IActionResult> BillingStatement(int? clientId)
        {
            // Get all clients for dropdown
            ViewBag.Clients = await _context.Clients
                .OrderBy(c => c.FullName)
                .ToListAsync();

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
                // Calculate total payments for this billing
                var paid = await _context.Payments
                    .Where(p => p.BillingId == billing.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var balance = billing.TotalAmount - paid;

                // Get service names from BookingDetails
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

            // Calculate totals
            statement.TotalBilling = statement.Items.Sum(x => x.Amount);
            statement.TotalPaid = statement.Items.Sum(x => x.Paid);
            statement.Balance = statement.Items.Sum(x => x.Balance);

            return View(statement);
        }
    }
}