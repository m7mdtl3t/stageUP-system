using System.Text;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Microsoft.AspNetCore.Authorization;

using VivuqeQRSystem.Services;

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class SeniorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public SeniorsController(ApplicationDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(int? eventId, int pageNumber = 1)
        {
            const int pageSize = 12;
            var query = _context.Seniors.AsNoTracking();
            
            if (eventId.HasValue)
            {
                query = query.Where(s => s.EventId == eventId);
                ViewBag.EventId = eventId;
            }
            
            var paginatedSeniors = await PaginatedList<Senior>.CreateAsync(
                query.OrderBy(s => s.Name), pageNumber, pageSize);
            
            return View(paginatedSeniors);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create(int? eventId)
        {
            if (eventId.HasValue)
            {
                ViewBag.EventId = eventId;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Create([Bind("Name,NumberOfGuests,EventId,PhoneNumber")] Senior senior)
        {
            if (!ModelState.IsValid) return View(senior);
            _context.Seniors.Add(senior);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Create", "Senior", senior.SeniorId.ToString(), $"Created senior '{senior.Name}'");
            return RedirectToAction(nameof(Details), new { id = senior.SeniorId });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var senior = await _context.Seniors.FindAsync(id);
            if (senior == null) return NotFound();
            return View(senior);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("SeniorId,Name,NumberOfGuests,PhoneNumber")] Senior senior)
        {
            if (id != senior.SeniorId) return NotFound();
            if (!ModelState.IsValid) return View(senior);
            _context.Update(senior);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Update", "Senior", senior.SeniorId.ToString(), $"Updated senior '{senior.Name}'");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var senior = await _context.Seniors.FindAsync(id);
            if (senior != null)
            {
                _context.Seniors.Remove(senior);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Delete", "Senior", id.ToString(), $"Deleted senior '{senior.Name}'");
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var senior = await _context.Seniors
                .Include(s => s.Guests)
                .FirstOrDefaultAsync(s => s.SeniorId == id);
            if (senior == null) return NotFound();

            // Build public URL
            var publicHost = _configuration["PublicHost"];
            var host = !string.IsNullOrWhiteSpace(publicHost) ? publicHost : $"http://{Request.Host}";
            var url = $"{host}/Seniors/Details/{id}";
            senior.QrUrl = url;

            return View(senior);
        }

        public async Task<IActionResult> QrImage(int id)
        {
            var senior = await _context.Seniors.FindAsync(id);
            if (senior == null) return NotFound();

            var publicHost = _configuration["PublicHost"];
            var host = !string.IsNullOrWhiteSpace(publicHost) ? publicHost : $"http://{Request.Host}";
            var url = $"{host}/Seniors/Details/{id}";

            using var generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrPng = new PngByteQRCode(data);
            byte[] pngBytes = qrPng.GetGraphic(20);

            var safeName = string.Join("_", (senior.Name ?? $"Senior_{id}").Split(Path.GetInvalidFileNameChars()));
            var downloadName = $"{safeName}.png";

            return File(pngBytes, "image/png", downloadName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(int guestId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null) return NotFound();

            guest.IsAttended = true;
            guest.AttendanceTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Attendance", "Guest", guest.GuestId.ToString(), $"Marked attendance for guest '{guest.Name}' (Senior ID: {guest.SeniorId})");

            var seniorId = guest.SeniorId;
            return RedirectToAction(nameof(Details), new { id = seniorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnmarkAttendance(int guestId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null) return NotFound();

            guest.IsAttended = false;
            guest.AttendanceTime = null;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Attendance", "Guest", guest.GuestId.ToString(), $"Unmarked attendance for guest '{guest.Name}' (Senior ID: {guest.SeniorId})");

            var seniorId = guest.SeniorId;
            return RedirectToAction(nameof(Details), new { id = seniorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddGuest(int seniorId, string name, string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction(nameof(Details), new { id = seniorId });
            }

            var senior = await _context.Seniors.Include(s => s.Guests).FirstOrDefaultAsync(s => s.SeniorId == seniorId);
            if (senior == null)
            {
                return NotFound();
            }

            var currentGuests = senior.Guests.Count;
            if (currentGuests >= senior.NumberOfGuests)
            {
                TempData["GuestLimit"] = $"Guest limit reached ({senior.NumberOfGuests}).";
                return RedirectToAction(nameof(Details), new { id = seniorId });
            }

            _context.Guests.Add(new Guest { Name = name.Trim(), PhoneNumber = phoneNumber, SeniorId = seniorId });
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Create", "Guest", "New", $"Added guest '{name}' to Senior ID {seniorId}");
            return RedirectToAction(nameof(Details), new { id = seniorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGuest(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();
            var seniorId = guest.SeniorId;
            var guestName = guest.Name;
            _context.Guests.Remove(guest);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Delete", "Guest", id.ToString(), $"Deleted guest '{guestName}' from Senior ID {seniorId}");
            return RedirectToAction(nameof(Details), new { id = seniorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditGuest(int id, string name, string? phoneNumber)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();
            var seniorId = guest.SeniorId;

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["GuestEditError"] = "Guest name is required.";
                return RedirectToAction(nameof(Details), new { id = seniorId });
            }

            guest.Name = name.Trim();
            guest.PhoneNumber = phoneNumber;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = seniorId });
        }
    }
}
