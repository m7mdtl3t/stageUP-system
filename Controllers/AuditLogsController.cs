using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivuqeQRSystem.Data;
using VivuqeQRSystem.Models;

namespace VivuqeQRSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuditLogs
        public async Task<IActionResult> Index(string? search, string? entity, string? actionType, int pageNumber = 1)
        {
            ViewBag.Search = search;
            ViewBag.Entity = entity;
            ViewBag.ActionType = actionType;

            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.Username.Contains(search) || l.Details.Contains(search) || l.EntityId.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(entity))
            {
                query = query.Where(l => l.EntityName == entity);
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(l => l.Action == actionType);
            }

            query = query.OrderByDescending(l => l.Timestamp);

            const int pageSize = 20;
            var paginatedLogs = await PaginatedList<AuditLog>.CreateAsync(query, pageNumber, pageSize);

            return View(paginatedLogs);
        }
    }
}
