using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get active event(s)
            var activeEvent = await _context.Events
                .Where(e => e.IsActive)
                .Include(e => e.Seniors)
                    .ThenInclude(s => s.Guests)
                .FirstOrDefaultAsync();

            ViewBag.ActiveEvent = activeEvent;
            
            if (activeEvent != null)
            {
                ViewBag.TotalSeniors = activeEvent.Seniors.Count;
                ViewBag.TotalGuests = activeEvent.Seniors.Sum(s => s.Guests.Count);
                ViewBag.AttendedGuests = activeEvent.Seniors.Sum(s => s.Guests.Count(g => g.IsAttended));
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error");
        }
    }
}
