using Microsoft.AspNetCore.Authentication.Cookies; // Dodaj ten using
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;

namespace RestaurantManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Po��czenie do bazy danych
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Dodaj kontrolery + widoki
            builder.Services.AddControllersWithViews();

            // Dodaj sesj�
            builder.Services.AddSession();

            // *** POPRAWKA 1: Rejestracja HttpContextAccessor ***
            // (Wymagane przez _Layout.cshtml do czytania Context.Session)
            builder.Services.AddHttpContextAccessor();

            // *** POPRAWKA 2: Dodanie serwis�w Autentykacji ***
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login"; // Strona logowania
                    options.AccessDeniedPath = "/Auth/AccessDenied"; // Strona braku dost�pu
                });


            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession(); // Musi by� przed UseAuthentication i UseAuthorization

            // *** POPRAWKA 3: Dodanie middleware Autentykacji ***
            // Musi by� wywo�ane PRZED UseAuthorization
            app.UseAuthentication();
            app.UseAuthorization(); // To ju� mia�e�


            // Domy�lna trasa
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Seed bazy przy starcie
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AppDbContext>();
                DatabaseSeeder.Seed(context);
            }

            app.Run();
        }
    }
}