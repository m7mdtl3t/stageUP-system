using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;

namespace VivuqeQRSystem.Controllers
{
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Single optimized query with AsNoTracking
            var events = await _context.Events
                .Include(e => e.Seniors)
                    .ThenInclude(s => s.Guests)
                .AsNoTracking()
                .ToListAsync();

            // Calculate totals from the single query (no extra DB calls)
            var allSeniors = events.SelectMany(e => e.Seniors).ToList();
            var allGuests = allSeniors.SelectMany(s => s.Guests).ToList();

            var viewModel = new StatisticsViewModel
            {
                TotalEvents = events.Count,
                TotalSeniors = allSeniors.Count,
                TotalGuests = allGuests.Count,
                TotalAttendedGuests = allGuests.Count(g => g.IsAttended),
                TotalPendingGuests = allGuests.Count(g => !g.IsAttended),
                ActiveEvents = events.Count(e => e.IsActive),
                OverallAttendanceRate = allGuests.Count > 0 
                    ? Math.Round((double)allGuests.Count(g => g.IsAttended) / allGuests.Count * 100, 1) 
                    : 0
            };

            // Per Event Statistics
            foreach (var evt in events.OrderByDescending(e => e.Date))
            {
                var eventGuests = evt.Seniors.SelectMany(s => s.Guests).ToList();
                
                viewModel.EventStats.Add(new EventStatistics
                {
                    EventId = evt.EventId,
                    EventName = evt.Name,
                    EventDate = evt.Date,
                    Location = evt.Location,
                    IsActive = evt.IsActive,
                    SeniorsCount = evt.Seniors.Count,
                    GuestsCount = eventGuests.Count,
                    AttendedGuests = eventGuests.Count(g => g.IsAttended),
                    PendingGuests = eventGuests.Count(g => !g.IsAttended),
                    AttendanceRate = eventGuests.Count > 0 
                        ? Math.Round((double)eventGuests.Count(g => g.IsAttended) / eventGuests.Count * 100, 1) 
                        : 0
                });
            }

            return View(viewModel);
        }
    }
}
