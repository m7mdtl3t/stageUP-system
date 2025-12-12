using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using ExcelDataReader;

using VivuqeQRSystem.Services;

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditService _auditService;

        public EventsController(ApplicationDbContext context, IWebHostEnvironment environment, IAuditService auditService)
        {
            _context = context;
            _environment = environment;
            _auditService = auditService;
        }

        // GET: Events (Dashboard)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Seniors)
                .ThenInclude(s => s.Guests)
                .AsNoTracking()
                .ToListAsync();
            
            // Check for legacy data (Seniors without EventId)
            var legacyCount = await _context.Seniors.CountAsync(s => s.EventId == null);
            ViewBag.LegacyCount = legacyCount;

            return View(events);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Role Check: Senior can only view their assigned event
            if (User.IsInRole("Senior"))
            {
                var username = User.Identity?.Name;
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || user.AssignedEventId != id)
                {
                    return Forbid(); // Or redirect to their assigned event
                }
            }

            var @event = await _context.Events
                .Include(e => e.Seniors)
                .ThenInclude(s => s.Guests)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // GET: Events/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("EventId,Name,Date,Location,IsActive")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Create", "Event", @event.EventId.ToString(), $"Created event '{@event.Name}'");
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();
            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,Name,Date,Location,IsActive")] Event @event)
        {
            if (id != @event.EventId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                    await _auditService.LogAsync("Update", "Event", @event.EventId.ToString(), $"Updated event '{@event.Name}'");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.EventId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Seniors)
                    .ThenInclude(s => s.Guests)
                .FirstOrDefaultAsync(e => e.EventId == id);
            
            if (@event != null)
            {
                // First, delete all guests for each senior
                foreach (var senior in @event.Seniors)
                {
                    _context.Guests.RemoveRange(senior.Guests);
                }
                
                // Then, delete all seniors
                _context.Seniors.RemoveRange(@event.Seniors);
                
                // Finally, delete the event
                _context.Events.Remove(@event);
                
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Delete", "Event", id.ToString(), $"Deleted event '{@event.Name}' with {@event.Seniors.Count} seniors");
            }
            return RedirectToAction(nameof(Index));
        }
        
        // POST: Events/MigrateLegacy
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MigrateLegacy()
        {
            // Find or Create "Legacy Event"
            var legacyEvent = await _context.Events.FirstOrDefaultAsync(e => e.Name == "Legacy Data");
            if (legacyEvent == null)
            {
                legacyEvent = new Event 
                { 
                    Name = "Legacy Data", 
                    Date = DateTime.Now, 
                    Location = "Migrated",
                    IsActive = true
                };
                _context.Events.Add(legacyEvent);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Create", "Event", legacyEvent.EventId.ToString(), "Auto-created Legacy Data event");
            }

            // Move duplicate seniors
            var seniors = await _context.Seniors.Where(s => s.EventId == null).ToListAsync();
            foreach (var s in seniors)
            {
                s.EventId = legacyEvent.EventId;
            }
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }

        // GET: Events/ShareLinks/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ShareLinks(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Seniors)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == id);
            
            if (@event == null) return NotFound();

            ViewBag.BaseUrl = $"{Request.Scheme}://{Request.Host}";
            return View(@event);
        }

        // POST: Events/GenerateTokens/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateTokens(int id)
        {
            var seniors = await _context.Seniors
                .Where(s => s.EventId == id)
                .ToListAsync();

            foreach (var senior in seniors)
            {
                if (string.IsNullOrEmpty(senior.ShareToken))
                {
                    senior.ShareToken = Guid.NewGuid().ToString("N").Substring(0, 10);
                }
            }

            await _context.SaveChangesAsync();
            await _auditService.LogAsync("GenerateTokens", "Event", id.ToString(), $"Generated share tokens for {seniors.Count} seniors");

            return RedirectToAction(nameof(ShareLinks), new { id });
        }

        // GET: Events/TicketSettings/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TicketSettings(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/TicketSettings/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TicketSettings(int id, [Bind("EventId,TicketTitle,TicketDateDisplay,TicketLocationDisplay,TicketMapUrl,TicketTimeDisplay,TicketWelcomeMessage")] Event eventSettings)
        {
            if (id != eventSettings.EventId) return NotFound();

            var eventToUpdate = await _context.Events.FindAsync(id);
            if (eventToUpdate == null) return NotFound();

            // validation workaround: we don't bind Name/Date so they will be invalid
            ModelState.Remove("Name");
            ModelState.Remove("Date");
            ModelState.Remove("Location"); // Optional but safer to remove if checked

            if (ModelState.IsValid)
            {
                // Only update ticket properties
                eventToUpdate.TicketTitle = eventSettings.TicketTitle;
                eventToUpdate.TicketDateDisplay = eventSettings.TicketDateDisplay;
                eventToUpdate.TicketLocationDisplay = eventSettings.TicketLocationDisplay;
                eventToUpdate.TicketMapUrl = eventSettings.TicketMapUrl;
                eventToUpdate.TicketTimeDisplay = eventSettings.TicketTimeDisplay;
                eventToUpdate.TicketWelcomeMessage = eventSettings.TicketWelcomeMessage;

                try
                {
                    _context.Update(eventToUpdate);
                    await _context.SaveChangesAsync();
                    await _auditService.LogAsync("Update", "Event", id.ToString(), "Updated ticket customization settings");
                    TempData["SuccessMessage"] = "Ticket settings updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(eventToUpdate.EventId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(TicketSettings), new { id });
            }
            return View(eventToUpdate);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Import(int eventId)
        {
            var evt = _context.Events.Find(eventId);
            if (evt == null) return NotFound();
            ViewBag.EventName = evt.Name;
            ViewBag.EventId = eventId;
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult DownloadTemplate()
        {
            // Simple CSV template still available
            var csv = "Name,PhoneNumber\nJohn Doe,01012345678";
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "Seniors_Template.csv");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(int eventId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid file.");
                return Import(eventId);
            }

            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null) return NotFound();

            int seniorsCount = 0;
            int guestsCount = 0;
            var errors = new List<string>();

            // Ensure we can read modern Excel formats
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
                var result = reader.AsDataSet();

                if (result.Tables.Count < 2)
                {
                    ModelState.AddModelError("", "Excel file must have at least 2 sheets (Seniors, Guests).");
                    return Import(eventId);
                }

                var seniorsTable = result.Tables[0];
                var guestsTable = result.Tables[1];

                // Dictionary to map Excel Senior_ID to Created Senior Entity
                var seniorMap = new Dictionary<string, Senior>();

                // 1. Process Seniors (Sheet 1)
                // Expected columns: Senior_ID (0), Senior_Name (1), Senior_Phone (2)
                for (int i = 1; i < seniorsTable.Rows.Count; i++) // Start at 1 to skip header
                {
                    var row = seniorsTable.Rows[i];
                    var excelId = row[0]?.ToString()?.Trim();
                    var name = row[1]?.ToString()?.Trim();
                    var phone = row[2]?.ToString()?.Trim(); // User said 3rd column is phone

                    if (string.IsNullOrEmpty(excelId) || string.IsNullOrEmpty(name)) continue;

                    var senior = new Senior
                    {
                        Name = name,
                        PhoneNumber = formatPhone(phone),
                        EventId = eventId,
                        NumberOfGuests = 0 // Will count later
                    };

                    _context.Seniors.Add(senior);
                    await _context.SaveChangesAsync(); // Save to get the DB ID

                    if (!seniorMap.ContainsKey(excelId))
                    {
                        seniorMap.Add(excelId, senior);
                        seniorsCount++;
                    }
                }

                // 2. Process Guests (Sheet 2)
                // Expected columns: Guest_ID (0), Guest_Name (1), Guest_Phone (2), Senior_ID (3)
                for (int i = 1; i < guestsTable.Rows.Count; i++) // Start at 1 to skip header
                {
                    var row = guestsTable.Rows[i];
                    var guestName = row[1]?.ToString()?.Trim();
                    var guestPhone = row[2]?.ToString()?.Trim();
                    var seniorRefId = row[3]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(guestName) || string.IsNullOrEmpty(seniorRefId)) continue;

                    if (seniorMap.TryGetValue(seniorRefId, out var parentSenior))
                    {
                        var guest = new Guest
                        {
                            Name = guestName,
                            PhoneNumber = formatPhone(guestPhone),
                            SeniorId = parentSenior.SeniorId,
                            IsAttended = false
                        };
                        _context.Guests.Add(guest);
                        
                        // Increment senior's guest count
                        parentSenior.NumberOfGuests++;
                        guestsCount++;
                    }
                    else
                    {
                        errors.Add($"Guest {guestName} linked to unknown Senior ID {seniorRefId}");
                    }
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Import", "Seniors/Guests", eventId.ToString(), $"Imported {seniorsCount} seniors and {guestsCount} guests into Event ID {eventId}");
                
                TempData["ImportMessage"] = $"Imported {seniorsCount} seniors and {guestsCount} guests successfully.";
                if (errors.Any())
                {
                    TempData["ImportErrors"] = string.Join("; ", errors.Take(10));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error processing file: {ex.Message}");
                return Import(eventId);
            }

            return RedirectToAction("Details", new { id = eventId });
        }

        private string? formatPhone(string? phone)
        {
            if (string.IsNullOrEmpty(phone)) return null;
            // Basic cleanup if needed, but keeping original formatting is safer for now
            return phone;
        }
    }
}
