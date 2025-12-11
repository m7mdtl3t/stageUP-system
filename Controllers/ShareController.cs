using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;

namespace VivuqeQRSystem.Controllers
{
    // Public controller - no [Authorize] attribute
    public class ShareController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Share/{token}
        // Public page - no login required
        [Route("Share/{token}")]
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrEmpty(token))
                return NotFound();

            var senior = await _context.Seniors
                .Include(s => s.Guests)
                .Include(s => s.Event)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShareToken == token);

            if (senior == null)
                return NotFound();

            return View(senior);
        }

        // Generate tokens for all seniors in an event (Admin only)
        public static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 10);
        }
    }
}
