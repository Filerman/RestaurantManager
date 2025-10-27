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

        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ScheduleTemplate> ScheduleTemplates { get; set; }
        public DbSet<TemplateShiftSlot> TemplateShiftSlots { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacja 1:1 User -> Employee
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.UserId);

            // Relacja M:N Employee <-> PositionTag
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.PositionTags)
                .WithMany(t => t.Employees);

            // Precyzja dla decimal (już było)
            modelBuilder.Entity<Employee>()
                .Property(e => e.HourlyRate)
                .HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(18, 2)");

            // *** NOWE KONFIGURACJE RELACJI ***

            // Schedule (1) ma wiele Shifts (*)
            modelBuilder.Entity<Schedule>()
                .HasMany(s => s.Shifts)
                .WithOne(sh => sh.Schedule)
                .HasForeignKey(sh => sh.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade); // Usunięcie grafiku usuwa jego zmiany

            // User (1) (pracownik) może mieć wiele Shifts (*)
            // UWAGA: Nie ustawiamy Cascade Delete! Usunięcie użytkownika NIE powinno
            // automatycznie usuwać historii jego zmian w starych grafikach.
            // Można rozważyć ustawienie FK na NULL lub ręczne zarządzanie. Na razie zostawiamy Restrict.
            modelBuilder.Entity<User>()
                .HasMany(u => u.Shifts) // Dodaj ICollection<Shift> Shifts w modelu User.cs
                .WithOne(sh => sh.EmployeeUser)
                .HasForeignKey(sh => sh.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Nie usuwaj zmian po usunięciu Usera

            // ScheduleTemplate (1) ma wiele TemplateShiftSlots (*)
            modelBuilder.Entity<ScheduleTemplate>()
                .HasMany(t => t.ShiftSlots)
                .WithOne(sl => sl.ScheduleTemplate)
                .HasForeignKey(sl => sl.ScheduleTemplateId)
                .OnDelete(DeleteBehavior.Cascade); // Usunięcie szablonu usuwa jego sloty

            // TemplateShiftSlot (1) wskazuje na jeden PositionTag (*) (wymaganą rolę)
            // Relacja jest już zdefiniowana przez ForeignKey w modelu TemplateShiftSlot,
            // ale dla pewności można dodać:
            modelBuilder.Entity<TemplateShiftSlot>()
               .HasOne(sl => sl.RequiredPositionTag)
               .WithMany() // PositionTag nie potrzebuje kolekcji TemplateShiftSlots
               .HasForeignKey(sl => sl.PositionTagId)
               .OnDelete(DeleteBehavior.Restrict); // Nie usuwaj slotów, jeśli tag zostanie usunięty

            // Shift (1) opcjonalnie wskazuje na jeden PositionTag (*) (rolę na zmianie)
            modelBuilder.Entity<Shift>()
               .HasOne(sh => sh.ShiftPositionTag)
               .WithMany() // PositionTag nie potrzebuje kolekcji Shifts
               .HasForeignKey(sh => sh.PositionTagId)
               .OnDelete(DeleteBehavior.SetNull); // Jeśli tag zostanie usunięty, ustaw FK w zmianie na NULL

            // *** KONIEC NOWYCH KONFIGURACJI ***
        }
    }
}