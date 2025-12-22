using System;
using System.Collections.Generic;

namespace VivuqeQRSystem.Models
{
    public class AnalyticsDashboardViewModel
    {
        // Overview Statistics
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int TotalSeniors { get; set; }
        public int TotalGuests { get; set; }
        public int TotalAttended { get; set; }
        public double AttendanceRate { get; set; }

        // Current Event Stats (if selected)
        public Event? SelectedEvent { get; set; }
        public int EventSeniors { get; set; }
        public int EventGuests { get; set; }
        public int EventAttended { get; set; }
        public double EventAttendanceRate { get; set; }

        // Charts Data
        public List<HourlyAttendanceData> HourlyAttendance { get; set; } = new();
        public List<EventComparisonData> EventsComparison { get; set; } = new();
        public List<DailyAttendanceData> DailyAttendance { get; set; } = new();
        public AttendanceDistribution Distribution { get; set; } = new();

        // Top Performers
        public List<TopEventData> TopEvents { get; set; } = new();
        
        // Recent Activity
        public List<RecentAttendanceActivity> RecentActivity { get; set; } = new();
    }

    public class HourlyAttendanceData
    {
        public string Hour { get; set; } = string.Empty; // "14:00", "15:00", etc.
        public int Count { get; set; }
        public string EventName { get; set; } = string.Empty;
    }

    public class EventComparisonData
    {
        public string EventName { get; set; } = string.Empty;
        public int TotalGuests { get; set; }
        public int Attended { get; set; }
        public int NotAttended { get; set; }
        public double AttendanceRate { get; set; }
        public DateTime EventDate { get; set; }
    }

    public class DailyAttendanceData
    {
        public string Date { get; set; } = string.Empty; // "Dec 20", "Dec 21", etc.
        public int Count { get; set; }
    }

    public class AttendanceDistribution
    {
        public int Morning { get; set; }      // 6 AM - 12 PM
        public int Afternoon { get; set; }    // 12 PM - 6 PM
        public int Evening { get; set; }      // 6 PM - 12 AM
        public int Night { get; set; }        // 12 AM - 6 AM
    }

    public class TopEventData
    {
        public string EventName { get; set; } = string.Empty;
        public int AttendanceCount { get; set; }
        public double AttendanceRate { get; set; }
        public DateTime EventDate { get; set; }
    }

    public class RecentAttendanceActivity
    {
        public string GuestName { get; set; } = string.Empty;
        public string SeniorName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime AttendanceTime { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}
