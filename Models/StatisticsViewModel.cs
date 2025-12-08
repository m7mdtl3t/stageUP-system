namespace VivuqeQRSystem.Models
{
    public class StatisticsViewModel
    {
        // Overall Stats
        public int TotalEvents { get; set; }
        public int TotalSeniors { get; set; }
        public int TotalGuests { get; set; }
        public int TotalAttendedGuests { get; set; }
        public int TotalPendingGuests { get; set; }
        public double OverallAttendanceRate { get; set; }

        // Active Event Stats
        public int ActiveEvents { get; set; }

        // Per Event Statistics
        public List<EventStatistics> EventStats { get; set; } = new List<EventStatistics>();
    }

    public class EventStatistics
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public int SeniorsCount { get; set; }
        public int GuestsCount { get; set; }
        public int AttendedGuests { get; set; }
        public int PendingGuests { get; set; }
        public double AttendanceRate { get; set; }
    }
}
