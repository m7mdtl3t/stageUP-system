namespace VivuqeQRSystem.Models
{
    public class SearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<SeniorSearchResult> SeniorResults { get; set; } = new();
        public List<GuestSearchResult> GuestResults { get; set; } = new();
        public int TotalResults => SeniorResults.Count + GuestResults.Count;
    }

    public class SeniorSearchResult
    {
        public int SeniorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int NumberOfGuests { get; set; }
        public int GuestsCount { get; set; }
        public string? EventName { get; set; }
        public int? EventId { get; set; }
    }

    public class GuestSearchResult
    {
        public int GuestId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsAttended { get; set; }
        public DateTime? AttendanceTime { get; set; }
        
        // Linked Senior Info
        public int SeniorId { get; set; }
        public string SeniorName { get; set; } = string.Empty;
        public string? SeniorPhoneNumber { get; set; }
        
        // Event Info
        public string? EventName { get; set; }
        public int? EventId { get; set; }
    }
}
