using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string q)
        {
            var viewModel = new SearchResultViewModel { Query = q ?? string.Empty };

            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return View(viewModel);
            }

            var searchTerm = q.Trim().ToLower();

            // Search Seniors
            var seniors = await _context.Seniors
                .Include(s => s.Event)
                .Include(s => s.Guests)
                .Where(s => (s.Event != null && s.Event.IsActive) &&
                           (s.Name.ToLower().Contains(searchTerm) ||
                           (s.PhoneNumber != null && s.PhoneNumber.Contains(searchTerm))))
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            viewModel.SeniorResults = seniors.Select(s => new SeniorSearchResult
            {
                SeniorId = s.SeniorId,
                Name = s.Name,
                PhoneNumber = s.PhoneNumber,
                NumberOfGuests = s.NumberOfGuests,
                GuestsCount = s.Guests.Count,
                EventName = s.Event?.Name,
                EventId = s.EventId
            }).ToList();

            // Search Guests
            var guests = await _context.Guests
                .Include(g => g.Senior)
                    .ThenInclude(s => s!.Event)
                .Where(g => (g.Senior != null && g.Senior.Event != null && g.Senior.Event.IsActive) &&
                           (g.Name.ToLower().Contains(searchTerm) ||
                           (g.PhoneNumber != null && g.PhoneNumber.Contains(searchTerm))))
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            viewModel.GuestResults = guests.Select(g => new GuestSearchResult
            {
                GuestId = g.GuestId,
                Name = g.Name,
                PhoneNumber = g.PhoneNumber,
                IsAttended = g.IsAttended,
                AttendanceTime = g.AttendanceTime,
                SeniorId = g.SeniorId,
                SeniorName = g.Senior?.Name ?? "Unknown",
                SeniorPhoneNumber = g.Senior?.PhoneNumber,
                EventName = g.Senior?.Event?.Name,
                EventId = g.Senior?.EventId
            }).ToList();

            return View(viewModel);
        }

        // API endpoint for real-time Senior search
        [HttpGet]
        public async Task<IActionResult> SearchSeniors(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { results = Array.Empty<object>() });
            }

            var searchTerm = q.Trim().ToLower();

            var seniors = await _context.Seniors
                .Include(s => s.Event)
                .Include(s => s.Guests)
                .Where(s => (s.Event != null && s.Event.IsActive) &&
                           (s.Name.ToLower().Contains(searchTerm) ||
                           (s.PhoneNumber != null && s.PhoneNumber.Contains(searchTerm))))
                .Take(10)
                .AsNoTracking()
                .Select(s => new
                {
                    s.SeniorId,
                    s.Name,
                    s.PhoneNumber,
                    s.NumberOfGuests,
                    GuestsCount = s.Guests.Count,
                    EventName = s.Event != null ? s.Event.Name : null,
                    EventId = s.EventId
                })
                .ToListAsync();

            return Json(new { results = seniors });
        }

        // API endpoint for real-time Guest search
        [HttpGet]
        public async Task<IActionResult> SearchGuests(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { results = Array.Empty<object>() });
            }

            var searchTerm = q.Trim().ToLower();

            var guests = await _context.Guests
                .Include(g => g.Senior)
                    .ThenInclude(s => s!.Event)
                .Where(g => (g.Senior != null && g.Senior.Event != null && g.Senior.Event.IsActive) &&
                           (g.Name.ToLower().Contains(searchTerm) ||
                           (g.PhoneNumber != null && g.PhoneNumber.Contains(searchTerm))))
                .Take(10)
                .AsNoTracking()
                .Select(g => new
                {
                    g.GuestId,
                    g.Name,
                    g.PhoneNumber,
                    g.IsAttended,
                    g.SeniorId,
                    SeniorName = g.Senior != null ? g.Senior.Name : "Unknown",
                    SeniorPhone = g.Senior != null ? g.Senior.PhoneNumber : null,
                    EventName = g.Senior != null && g.Senior.Event != null ? g.Senior.Event.Name : null,
                    EventId = g.Senior != null ? g.Senior.EventId : null
                })
                .ToListAsync();

            return Json(new { results = guests });
        }

        // Unified search - returns both seniors and guests with type labels
        [HttpGet]
        public async Task<IActionResult> SearchAll(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Json(new { results = Array.Empty<object>() });
            }

            var searchTerm = q.Trim().ToLower();

            // Search Seniors
            var seniors = await _context.Seniors
                .Include(s => s.Event)
                .Where(s => (s.Event != null && s.Event.IsActive) &&
                           (s.Name.ToLower().Contains(searchTerm) ||
                           (s.PhoneNumber != null && s.PhoneNumber.Contains(searchTerm))))
                .Take(5)
                .AsNoTracking()
                .Select(s => new
                {
                    Id = s.SeniorId,
                    s.Name,
                    s.PhoneNumber,
                    Type = "Senior",
                    Subtitle = s.Event != null ? s.Event.Name : "No Event",
                    Link = "/Seniors/Details/" + s.SeniorId,
                    IsAttended = (bool?)null
                })
                .ToListAsync();

            // Search Guests
            var guests = await _context.Guests
                .Include(g => g.Senior)
                    .ThenInclude(s => s!.Event)
                .Where(g => (g.Senior != null && g.Senior.Event != null && g.Senior.Event.IsActive) &&
                           (g.Name.ToLower().Contains(searchTerm) ||
                           (g.PhoneNumber != null && g.PhoneNumber.Contains(searchTerm))))
                .Take(5)
                .AsNoTracking()
                .Select(g => new
                {
                    Id = g.GuestId,
                    g.Name,
                    g.PhoneNumber,
                    Type = "Guest",
                    Subtitle = g.Senior != null ? "→ " + g.Senior.Name : "Unknown Senior",
                    Link = "/Seniors/Details/" + g.SeniorId,
                    IsAttended = (bool?)g.IsAttended
                })
                .ToListAsync();

            // Combine and return
            var results = seniors.Cast<object>().Concat(guests.Cast<object>()).ToList();
            return Json(new { results });
        }
    }
}
