using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;
using VivuqeQRSystem.Services;

namespace VivuqeQRSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UsersController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            
            // Get activity stats for each user from AuditLogs
            var markStats = await _context.AuditLogs
                .Where(a => a.Action == "Mark" || a.Action == "Attendance") // Include old records
                .GroupBy(a => a.Username)
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Username, x => x.Count);
            
            var unmarkStats = await _context.AuditLogs
                .Where(a => a.Action == "Unmark")
                .GroupBy(a => a.Username)
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Username, x => x.Count);
            
            var loginStats = await _context.AuditLogs
                .Where(a => a.Action == "Login")
                .GroupBy(a => a.Username)
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Username, x => x.Count);
            
            var totalActions = await _context.AuditLogs
                .GroupBy(a => a.Username)
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Username, x => x.Count);
            
            ViewBag.MarkStats = markStats;
            ViewBag.UnmarkStats = unmarkStats;
            ViewBag.LoginStats = loginStats;
            ViewBag.TotalActions = totalActions;
            
            return View(users);
        }

        // GET: Users/Activity/username
        public async Task<IActionResult> Activity(string username)
        {
            if (string.IsNullOrEmpty(username))
                return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();

            // Get all attendance-related actions for this user
            var attendanceLogs = await _context.AuditLogs
                .Where(a => a.Username == username && (a.Action == "Mark" || a.Action == "Unmark" || a.Action == "Attendance"))
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.MarkCount = attendanceLogs.Count(a => a.Action == "Mark" || a.Action == "Attendance");
            ViewBag.UnmarkCount = attendanceLogs.Count(a => a.Action == "Unmark");
            
            return View(attendanceLogs);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewBag.Events = _context.Events.Select(e => new { e.EventId, e.Name }).ToList();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Password,Role,AssignedEventId")] User user)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    ViewBag.Events = _context.Events.Select(e => new { e.EventId, e.Name }).ToList();
                    return View(user);
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                
                // Log Audit
                await _auditService.LogAsync("Create", "User", user.Id.ToString(), $"Created new user '{user.Username}' with role '{user.Role}'");
                
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Events = _context.Events.Select(e => new { e.EventId, e.Name }).ToList();
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            ViewBag.Events = _context.Events.Select(e => new { e.EventId, e.Name }).ToList();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Password,Role,AssignedEventId")] User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    
                    // Log Audit
                    await _auditService.LogAsync("Update", "User", user.Id.ToString(), $"Updated user '{user.Username}'");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Events = _context.Events.Select(e => new { e.EventId, e.Name }).ToList();
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            var currentUsername = User.Identity?.Name;

            if (user != null)
            {
                // Prevent deleting self
                if (user.Username == currentUsername)
                {
                    TempData["Error"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                // Log Audit
                await _auditService.LogAsync("Delete", "User", id.ToString(), $"Deleted user '{user.Username}'");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
