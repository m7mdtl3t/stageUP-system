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

        public virtual ICollection<Senior> Seniors { get; set; } = new List<Senior>();
    }
}
