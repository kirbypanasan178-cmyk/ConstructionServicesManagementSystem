using ConstructionServicesManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ConstructionServices.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Billing> Billings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Tool> Tools { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Service
            modelBuilder.Entity<Service>()
                .Property(s => s.HourlyRate)
                .HasPrecision(18, 2);

            // Booking
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            // Booking Detail
            modelBuilder.Entity<BookingDetail>()
                .Property(bd => bd.HoursRendered)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BookingDetail>()
                .Property(bd => bd.HourlyRate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BookingDetail>()
                .Property(bd => bd.Amount)
                .HasPrecision(18, 2);

            // Billing
            modelBuilder.Entity<Billing>()
                .Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Billing>()
                .Property(b => b.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Billing>()
                .Property(b => b.Balance)
                .HasPrecision(18, 2);

            // Payment
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // Booking -> Client
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Client)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookingDetail -> Booking
            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingDetail -> Service
            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Service)
                .WithMany()
                .HasForeignKey(bd => bd.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Billing -> Booking
            modelBuilder.Entity<Billing>()
                .HasOne(b => b.Booking)
                .WithOne(b => b.Billing)
                .HasForeignKey<Billing>(b => b.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment -> Billing
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Billing)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BillingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tool -> Service
            modelBuilder.Entity<Tool>()
                .HasOne(t => t.Service)
                .WithMany()
                .HasForeignKey(t => t.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Initial Services
            modelBuilder.Entity<Service>().HasData(
                new Service
                {
                    Id = 1,
                    Name = "Plumbing",
                    HourlyRate = 500,
                    IsActive = true
                },
                new Service
                {
                    Id = 2,
                    Name = "Electrical",
                    HourlyRate = 600,
                    IsActive = true
                },
                new Service
                {
                    Id = 3,
                    Name = "Masonry",
                    HourlyRate = 550,
                    IsActive = true
                },
                new Service
                {
                    Id = 4,
                    Name = "Carpentry Works",
                    HourlyRate = 450,
                    IsActive = true
                },
                new Service
                {
                    Id = 5,
                    Name = "Others",
                    HourlyRate = 400,
                    IsActive = true
                }
            );
        }
    }
}