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

        // --- Twoje istniejące tabele ---
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
        public DbSet<LossLog> LossLogs { get; set; }

        // --- NOWOŚĆ: Tabela godzin otwarcia ---
        public DbSet<OpeningHour> OpeningHours { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Twoje istniejące konfiguracje ---

            // Relacja 1:1 User -> Employee
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.UserId);

            // Relacja M:N Employee <-> PositionTag
            modelBuilder.Entity<Employee>()
                .HasMany(e => e.PositionTags)
                .WithMany(t => t.Employees);

            // Precyzja dla decimal
            modelBuilder.Entity<Employee>()
                .Property(e => e.HourlyRate)
                .HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(18, 2)");

            // Schedule (1) ma wiele Shifts (*)
            modelBuilder.Entity<Schedule>()
                .HasMany(s => s.Shifts)
                .WithOne(sh => sh.Schedule)
                .HasForeignKey(sh => sh.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            // User (1) (pracownik) może mieć wiele Shifts (*)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Shifts)
                .WithOne(sh => sh.EmployeeUser)
                .HasForeignKey(sh => sh.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ScheduleTemplate (1) ma wiele TemplateShiftSlots (*)
            modelBuilder.Entity<ScheduleTemplate>()
                .HasMany(t => t.ShiftSlots)
                .WithOne(sl => sl.ScheduleTemplate)
                .HasForeignKey(sl => sl.ScheduleTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // TemplateShiftSlot (1) wskazuje na jeden PositionTag (*)
            modelBuilder.Entity<TemplateShiftSlot>()
               .HasOne(sl => sl.RequiredPositionTag)
               .WithMany()
               .HasForeignKey(sl => sl.PositionTagId)
               .OnDelete(DeleteBehavior.Restrict);

            // Shift (1) opcjonalnie wskazuje na jeden PositionTag (*)
            modelBuilder.Entity<Shift>()
               .HasOne(sh => sh.ShiftPositionTag)
               .WithMany()
               .HasForeignKey(sh => sh.PositionTagId)
               .OnDelete(DeleteBehavior.SetNull);


            // --- DODATKOWA KONFIGURACJA DLA NOWYCH FUNKCJI ---

            // Relacja User -> Reservation (Klient) - potrzebne do filtrowania rezerwacji gościa
            modelBuilder.Entity<Reservation>()
               .HasOne(r => r.User)
               .WithMany(u => u.Reservations)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}