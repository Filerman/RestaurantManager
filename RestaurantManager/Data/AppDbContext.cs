using Microsoft.EntityFrameworkCore;
using RestaurantManager.Models;

namespace RestaurantManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Availability> Availabilities { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<PositionTag> PositionTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracja relacji 1:1 między User i Employee
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.UserId);

            // Konfiguracja relacji M:N między Employee i PositionTag
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.PositionTags)
                .WithMany(t => t.Employees);

            // *** NOWA KONFIGURACJA - Naprawia ostrzeżenia decimal ***
            modelBuilder.Entity<Employee>()
                .Property(e => e.HourlyRate)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(18, 2)");
        }
    }
}