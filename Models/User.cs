using System.ComponentModel.DataAnnotations;

namespace VivuqeQRSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "User"; // Default to User

        public int? AssignedEventId { get; set; }
        public Event? AssignedEvent { get; set; }
    }
}
