using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;

namespace RestaurantManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Pobieramy ustawienia strony g³ównej wraz ze zdjêciami karuzeli
            var settings = await _context.HomePageSettings
                .Include(x => x.CarouselImages)
                .FirstOrDefaultAsync();

            // Jeœli nie ma ustawieñ w bazie (pierwsze uruchomienie), tworzymy domyœlny obiekt
            if (settings == null)
            {
                settings = new HomePageSetting
                {
                    HeroTitle = "Najlepsze smaki w mieœcie.",
                    HeroSubtitle = "Odkryj nasze menu i zarezerwuj stolik na wyj¹tkowy wieczór."
                };
            }

            var username = HttpContext.Session.GetString("Username");
            ViewBag.Username = username;

            return View(settings);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}