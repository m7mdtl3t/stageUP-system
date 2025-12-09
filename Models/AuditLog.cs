using System;
using System.ComponentModel.DataAnnotations;

namespace VivuqeQRSystem.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string Action { get; set; } // Create, Update, Delete, Login
        public string EntityName { get; set; } // Senior, Guest, Event, User
        public string EntityId { get; set; }   // ID of the entity
        
        public string Username { get; set; }   // Who performed the action
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public string Details { get; set; }    // JSON or description of changes
    }
}
