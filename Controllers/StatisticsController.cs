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

        // GET: Statistics/Dashboard
        public async Task<IActionResult> Dashboard(int? eventId = null)
        {
            var viewModel = new AnalyticsDashboardViewModel();

            // Get all events with related data
            var eventsQuery = _context.Events
                .Include(e => e.Seniors)
                    .ThenInclude(s => s.Guests)
                .AsNoTracking();

            var allEvents = await eventsQuery.ToListAsync();

            // Determine Scope (Filtered or All)
            var scopeEvents = eventId.HasValue
                ? allEvents.Where(e => e.EventId == eventId.Value).ToList()
                : allEvents;

            var scopeSeniors = scopeEvents.SelectMany(e => e.Seniors).ToList();
            var scopeGuests = scopeSeniors.SelectMany(s => s.Guests).ToList();

            // 1. Overview Cards (Based on Scope)
            viewModel.TotalEvents = scopeEvents.Count;
            viewModel.ActiveEvents = scopeEvents.Count(e => e.IsActive);
            viewModel.TotalSeniors = scopeSeniors.Count;
            viewModel.TotalGuests = scopeGuests.Count;
            viewModel.TotalAttended = scopeGuests.Count(g => g.IsAttended);
            viewModel.AttendanceRate = scopeGuests.Count > 0
                ? Math.Round((double)viewModel.TotalAttended / scopeGuests.Count * 100, 1)
                : 0;

            // Selected Event Data (for Dropdown Selection State)
            if (eventId.HasValue)
            {
                viewModel.SelectedEvent = allEvents.FirstOrDefault(e => e.EventId == eventId.Value);
            }

            // 2. Hourly Attendance Analysis (Based on Scope)
            var scopeAttendedGuests = scopeGuests.Where(g => g.IsAttended && g.AttendanceTime.HasValue).ToList();

            viewModel.HourlyAttendance = scopeAttendedGuests
                .GroupBy(g => g.AttendanceTime!.Value.Hour)
                .Select(group => new HourlyAttendanceData
                {
                    Hour = $"{group.Key:D2}:00",
                    Count = group.Count(),
                    EventName = eventId.HasValue ? viewModel.SelectedEvent?.Name ?? "" : "All Events"
                })
                .OrderBy(h => h.Hour)
                .ToList();

            // 3. Events Comparison (Always show top events for context, or filter? usually comparison implies multiple)
            // Let's keep showing All Events for comparison chart to see how this event compares to others
            viewModel.EventsComparison = allEvents
                .Select(e =>
                {
                    var guests = e.Seniors.SelectMany(s => s.Guests).ToList();
                    var attended = guests.Count(g => g.IsAttended);
                    
                    return new EventComparisonData
                    {
                        EventName = e.Name,
                        TotalGuests = guests.Count,
                        Attended = attended,
                        NotAttended = guests.Count - attended,
                        AttendanceRate = guests.Count > 0 ? Math.Round((double)attended / guests.Count * 100, 1) : 0,
                        EventDate = e.Date
                    };
                })
                .OrderByDescending(e => e.EventDate)
                .Take(10)
                .ToList();

            // 4. Daily Attendance (Based on Scope)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            viewModel.DailyAttendance = scopeAttendedGuests
                .Where(g => g.AttendanceTime!.Value >= thirtyDaysAgo)
                .GroupBy(g => g.AttendanceTime!.Value.Date)
                .Select(group => new DailyAttendanceData
                {
                    Date = group.Key.ToString("MMM dd"),
                    Count = group.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // 5. Attendance Distribution (Based on Scope)
            viewModel.Distribution = new AttendanceDistribution
            {
                Morning = scopeAttendedGuests.Count(g => g.AttendanceTime!.Value.Hour >= 6 && g.AttendanceTime.Value.Hour < 12),
                Afternoon = scopeAttendedGuests.Count(g => g.AttendanceTime!.Value.Hour >= 12 && g.AttendanceTime.Value.Hour < 18),
                Evening = scopeAttendedGuests.Count(g => g.AttendanceTime!.Value.Hour >= 18 && g.AttendanceTime.Value.Hour < 24),
                Night = scopeAttendedGuests.Count(g => g.AttendanceTime!.Value.Hour >= 0 && g.AttendanceTime.Value.Hour < 6)
            };

            // 6. Top Events (Always global context)
            viewModel.TopEvents = viewModel.EventsComparison
                .OrderByDescending(e => e.Attended)
                .Take(5)
                .Select(e => new TopEventData
                {
                    EventName = e.EventName,
                    AttendanceCount = e.Attended,
                    AttendanceRate = e.AttendanceRate,
                    EventDate = e.EventDate
                })
                .ToList();

            // 7. Recent Activity (Based on Scope)
            viewModel.RecentActivity = scopeAttendedGuests
                .OrderByDescending(g => g.AttendanceTime)
                .Take(20)
                .Select(g =>
                {
                    var timeAgo = GetTimeAgo(g.AttendanceTime!.Value);
                    return new RecentAttendanceActivity
                    {
                        GuestName = g.Name,
                        SeniorName = g.Senior?.Name ?? "Unknown",
                        EventName = g.Senior?.Event?.Name ?? "Unknown",
                        AttendanceTime = g.AttendanceTime!.Value,
                        TimeAgo = timeAgo
                    };
                })
                .ToList();

            ViewBag.AllEvents = allEvents.OrderByDescending(e => e.Date).ToList();
            return View(viewModel);
        }

        private string GetTimeAgo(DateTime datetime)
        {
            var timeSpan = DateTime.Now - datetime;
            
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} min ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hr ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} days ago";
            
            return datetime.ToString("MMM dd, yyyy");
        }
    }
}
