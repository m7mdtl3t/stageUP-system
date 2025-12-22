using System.Text;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using Microsoft.AspNetCore.Authorization;

using VivuqeQRSystem.Services;

using VivuqeQRSystem.Services;
using Microsoft.AspNetCore.SignalR;
using VivuqeQRSystem.Hubs;

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class SeniorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;
        private readonly IHubContext<AttendanceHub> _hubContext;

        public SeniorsController(ApplicationDbContext context, IConfiguration configuration, IAuditService auditService, IHubContext<AttendanceHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
            _hubContext = hubContext;
        }

        [Authorize(Roles = "Admin")]
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
            
            // Get the existing senior to preserve EventId and ShareToken
            var existingSenior = await _context.Seniors.AsNoTracking().FirstOrDefaultAsync(s => s.SeniorId == id);
            if (existingSenior != null)
            {
                senior.EventId = existingSenior.EventId;
                senior.ShareToken = existingSenior.ShareToken;
            }
            
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

        public async Task<IActionResult> Details(int id, int? highlightGuest = null, bool autoMark = false)
        {
            var senior = await _context.Seniors
                .Include(s => s.Guests)
                .Include(s => s.Event)
                .FirstOrDefaultAsync(s => s.SeniorId == id);
            
            if (senior == null) return NotFound();

            // Role Check: Senior user can only view seniors in their assigned event
            if (User.IsInRole("Senior"))
            {
                var username = User.Identity?.Name;
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || user.AssignedEventId != senior.EventId)
                {
                    return Forbid();
                }
            }

            // AUTO-MARK LOGIC (Fixes the QR Download Bug)
            if (autoMark && highlightGuest.HasValue)
            {
                var guest = senior.Guests.FirstOrDefault(g => g.GuestId == highlightGuest.Value);
                if (guest != null && !guest.IsAttended)
                {
                    // Check Event Active Status
                    if (senior.Event != null && !senior.Event.IsActive)
                    {
                        TempData["Error"] = "❌ Check-in BLOCKED: Event is Inactive.";
                    }
                    else
                    {
                        // Mark Attended
                        guest.IsAttended = true;
                        guest.AttendanceTime = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await _auditService.LogAsync("AutoMark", "Guest", guest.GuestId.ToString(), $"Auto-marked attendance via QR for '{guest.Name}'");

                        // SignalR Update
                        var eventId = senior.EventId ?? 0;
                        var currentCount = await _context.Guests.Where(g => g.Senior.EventId == eventId && g.IsAttended).CountAsync();
                        var eventName = senior.Event?.Name ?? "Event";
                        
                        await _hubContext.Clients.Group(eventId.ToString()).SendAsync("ReceiveAttendanceUpdate", currentCount, eventId, guest.Name, "Just now", eventName);
                        await _hubContext.Clients.Group("GlobalMonitor").SendAsync("ReceiveAttendanceUpdate", currentCount, eventId, guest.Name, "Just now", eventName);
                        
                        TempData["SuccessMessage"] = $"✅ Welcome, {guest.Name}! Attendance marked.";
                    }
                }
            }

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
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> MarkAttendance(int guestId)
        {
            var guest = await _context.Guests
                .Include(g => g.Senior)
                .ThenInclude(s => s.Event)
                .FirstOrDefaultAsync(g => g.GuestId == guestId);

            if (guest == null) return NotFound();

            // LOGIC CHECK: Ensure Event is Active & Today
            var evt = guest.Senior?.Event;
            if (evt != null)
            {
                if (!evt.IsActive)
                {
                    TempData["Error"] = "❌ Check-in BLOCKED: Event is Inactive.";
                    return RedirectToAction(nameof(Details), new { id = guest.SeniorId });
                }
                
            }

            guest.IsAttended = true;
            guest.AttendanceTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Mark", "Guest", guest.GuestId.ToString(), $"Marked attendance for guest '{guest.Name}' (Senior ID: {guest.SeniorId})");

            // Real-time Update
            if (guest.Senior != null)
            {
                var eventId = guest.Senior.EventId ?? 0;
                var currentCount = await _context.Guests
                    .Where(g => g.Senior.EventId == eventId && g.IsAttended)
                    .CountAsync();
                
                var eventName = guest.Senior.Event?.Name ?? "Unknown Event";

                await _hubContext.Clients.Group(eventId.ToString()).SendAsync("ReceiveAttendanceUpdate", 
                    currentCount, 
                    eventId, 
                    guest.Name, 
                    "Just now", 
                    eventName);

                // Broadcast to Global Admin Listeners
                await _hubContext.Clients.Group("GlobalMonitor").SendAsync("ReceiveAttendanceUpdate", 
                    currentCount, 
                    eventId, 
                    guest.Name, 
                    "Just now", 
                    eventName);
            }

            var seniorId = guest.SeniorId;
            return RedirectToAction(nameof(Details), new { id = seniorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> UnmarkAttendance(int guestId)
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null) return NotFound();

            guest.IsAttended = false;
            guest.AttendanceTime = null;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Unmark", "Guest", guest.GuestId.ToString(), $"Unmarked attendance for guest '{guest.Name}' (Senior ID: {guest.SeniorId})");

            // Real-time Update (Decrement Count)
            var senior = await _context.Seniors.Include(s => s.Event).FirstOrDefaultAsync(s => s.SeniorId == guest.SeniorId);
            if (senior != null)
            {
                var eventId = senior.EventId ?? 0;
                var currentCount = await _context.Guests
                    .Where(g => g.Senior.EventId == eventId && g.IsAttended)
                    .CountAsync();
                
                var eventName = senior.Event?.Name ?? "Unknown Event";

                // Broadcast
                await _hubContext.Clients.Group(eventId.ToString()).SendAsync("ReceiveAttendanceUpdate", 
                    currentCount, 
                    eventId, 
                    guest.Name, 
                    "UNMARKED", // Special flag or just ignored by UI for table, but updates counter
                    eventName);

                await _hubContext.Clients.Group("GlobalMonitor").SendAsync("ReceiveAttendanceUpdate", 
                    currentCount, 
                    eventId, 
                    guest.Name, 
                    "UNMARKED", 
                    eventName);
            }

            return RedirectToAction(nameof(Details), new { id = guest.SeniorId });
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

        // GET: Seniors/ShareInvitations/5
        public async Task<IActionResult> ShareInvitations(int id)
        {
            var senior = await _context.Seniors
                .Include(s => s.Guests)
                .Include(s => s.Event)
                .FirstOrDefaultAsync(s => s.SeniorId == id);
            
            if (senior == null) return NotFound();

            // Ensure Ticket Tokens exist
            bool needsSave = false;
            foreach (var guest in senior.Guests)
            {
                if (string.IsNullOrEmpty(guest.TicketToken))
                {
                    guest.TicketToken = Guid.NewGuid().ToString("N");
                    needsSave = true;
                }
            }
            if (needsSave) await _context.SaveChangesAsync();

            return View(senior);
        }
    }
}
