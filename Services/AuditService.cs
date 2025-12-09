using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;

namespace VivuqeQRSystem.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string entityName, string entityId, string details)
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var log = new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Username = username,
                Timestamp = DateTime.Now,
                Details = details
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
