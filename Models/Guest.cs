using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VivuqeQRSystem.Models
{
    public class Guest
    {
        public int GuestId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        public bool IsAttended { get; set; }

        public DateTime? AttendanceTime { get; set; }

        [ForeignKey("Senior")]
        public int SeniorId { get; set; }
        public Senior? Senior { get; set; }

        [StringLength(50)]
        public string? TicketToken { get; set; } // New property for Digital Ticket
    }
}
