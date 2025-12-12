using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VivuqeQRSystem.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Custom Ticket Settings (Overrides)
        [Display(Name = "Ticket Title")]
        [StringLength(100)]
        public string? TicketTitle { get; set; } // Overrides Name

        [Display(Name = "Ticket Date Text")]
        [StringLength(50)]
        public string? TicketDateDisplay { get; set; } // Overrides formatted Date

        [Display(Name = "Ticket Location Text")]
        [StringLength(100)]
        public string? TicketLocationDisplay { get; set; } // Overrides Location

        [Display(Name = "Ticket Map URL")]
        [StringLength(500)]
        public string? TicketMapUrl { get; set; } // Overrides auto-generated map link

        [Display(Name = "Time Display")]
        [StringLength(50)]
        public string? TicketTimeDisplay { get; set; } // e.g. "6:00 PM (Doors close 9:00 PM)"

        [Display(Name = "Welcome Message")]
        [StringLength(200)]
        public string? TicketWelcomeMessage { get; set; } // e.g. "You’re exclusively invited to..."

        public virtual ICollection<Senior> Seniors { get; set; } = new List<Senior>();
    }
}
