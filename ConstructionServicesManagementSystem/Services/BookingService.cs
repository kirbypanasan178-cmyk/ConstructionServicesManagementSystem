using ConstructionServices.Data;
using ConstructionServicesManagementSystem.Enums;
using ConstructionServicesManagementSystem.Models;
using ConstructionServicesManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServicesManagementSystem.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Service)
                .Include(b => b.Billing)
                .OrderByDescending(b => b.VisitDate)
                .ToListAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Service)
                .Include(b => b.Billing)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> IsDateBookedAsync(DateTime visitDate)
        {
            return await _context.Bookings
                .AnyAsync(b => b.VisitDate.Date == visitDate.Date);
        }

        public async Task<Booking> CreateAsync(
            int clientId,
            DateTime visitDate,
            List<BookingDetail> bookingDetails)
        {
            // Check if date is already booked
            if (await IsDateBookedAsync(visitDate))
            {
                throw new InvalidOperationException(
                    "The selected date is already booked.");
            }

            // Make sure there is at least one service
            if (bookingDetails == null || bookingDetails.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one service is required.");
            }

            // Get selected services
            var serviceIds = bookingDetails
                .Select(d => d.ServiceId)
                .Distinct()
                .ToList();

            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
                .ToListAsync();

            // Validate selected services
            if (services.Count != serviceIds.Count)
            {
                throw new InvalidOperationException(
                    "One or more selected services are invalid or inactive.");
            }

            // Calculate each detail
            foreach (var detail in bookingDetails)
            {
                if (detail.HoursRendered <= 0)
                {
                    throw new InvalidOperationException(
                        "Hours rendered must be greater than zero.");
                }

                var service = services
                    .First(s => s.Id == detail.ServiceId);

                // Get current hourly rate from database
                detail.HourlyRate = service.HourlyRate;

                // Hours × Hourly Rate
                detail.Amount =
                    detail.HoursRendered * detail.HourlyRate;
            }

            // Calculate total amount
            var totalAmount = bookingDetails.Sum(d => d.Amount);

            // Create booking
            var booking = new Booking
            {
                ClientId = clientId,
                VisitDate = visitDate,
                TotalAmount = totalAmount,
                BookingDetails = bookingDetails
            };

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            // Generate billing transaction
            var billing = new Billing
            {
                BookingId = booking.Id,

                // Example billing number
                BillingNumber = $"BILL-{DateTime.Now:yyyyMMddHHmmss}",

                TotalAmount = booking.TotalAmount,
                AmountPaid = 0,
                Balance = booking.TotalAmount,

                Status = BillingStatus.Unpaid,

                CreatedAt = DateTime.UtcNow
            };

            _context.Billings.Add(billing);

            await _context.SaveChangesAsync();

            return booking;
        }
    }
}