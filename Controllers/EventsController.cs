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

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EventsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Events (Dashboard)
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.AsNoTracking().ToListAsync();
            
            // Check for legacy data (Seniors without EventId)
            var legacyCount = await _context.Seniors.CountAsync(s => s.EventId == null);
            ViewBag.LegacyCount = legacyCount;

            return View(events);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

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
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
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
            var csv = "Name,PhoneNumber,NumberOfGuests\nJohn Doe,01012345678,2\nJane Smith,01123456789,3";
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
                ModelState.AddModelError("", "Please upload a valid CSV file.");
                return Import(eventId);
            }

            var evt = await _context.Events.FindAsync(eventId);
            if (evt == null) return NotFound();

            int successCount = 0;
            int errorCount = 0;

            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                var headerLine = await stream.ReadLineAsync(); // Skip header

                while (!stream.EndOfStream)
                {
                    var line = await stream.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');
                    if (values.Length >= 1)
                    {
                        var name = values[0].Trim();
                        var phone = values.Length > 1 ? values[1].Trim() : null;
                        var guestsStr = values.Length > 2 ? values[2].Trim() : "0";
                        int guests = 0;
                        int.TryParse(guestsStr, out guests);

                        if (!string.IsNullOrEmpty(name))
                        {
                            var senior = new Senior
                            {
                                Name = name,
                                PhoneNumber = phone,
                                NumberOfGuests = guests,
                                EventId = eventId
                            };
                            _context.Seniors.Add(senior);
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                await _context.SaveChangesAsync();
                TempData["ImportMessage"] = $"Imported {successCount} seniors successfully. {errorCount} errors.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error parsing file: {ex.Message}");
                return Import(eventId);
            }

            return RedirectToAction("Details", new { id = eventId });
        }
    }
}
