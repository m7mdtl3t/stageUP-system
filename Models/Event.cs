using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

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

        [Display(Name = "Visual Message on Ticket")]
        [StringLength(200)]
        public string? TicketVisualMessage { get; set; } // e.g. "Doors Open 9PM"

        [Display(Name = "WhatsApp Template")]
        [StringLength(500)]
        public string? WhatsAppMessage { get; set; } // e.g. "Hi {GuestName}..."

        [Display(Name = "Link Preview Description")]
        [StringLength(200)]
        public string? LinkDescription { get; set; } // OG:Description

        [Display(Name = "Welcome Message (Legacy)")]
        [StringLength(200)]
        public string? TicketWelcomeMessage { get; set; } // Kept for backward compat, or repurposed

        // Branding Settings
        [Display(Name = "Primary Color")]
        [StringLength(7)] // Hex code #RRGGBB
        public string? PrimaryColor { get; set; } = "#6f42c1"; // Default purple

        [Display(Name = "Secondary Color")]
        [StringLength(7)]
        public string? SecondaryColor { get; set; } = "#0dcaf0"; // Default cyan

        [Display(Name = "Event Logo")]
        public string? LogoPath { get; set; } // Path to stored logo image

        [NotMapped]
        [Display(Name = "Upload Logo")]
        public IFormFile? LogoFile { get; set; }

        public virtual ICollection<Senior> Seniors { get; set; } = new List<Senior>();
    }
}
