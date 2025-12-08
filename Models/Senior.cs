using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VivuqeQRSystem.Models
{
    public class Senior
    {
        public int SeniorId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 1000)]
        public int NumberOfGuests { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? QrUrl { get; set; }

        public int? EventId { get; set; }
        public Event? Event { get; set; }

        public ICollection<Guest> Guests { get; set; } = new List<Guest>();
    }
}
