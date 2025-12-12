using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using VivuqeQRSystem.Data;
using Microsoft.Extensions.Configuration;

namespace VivuqeQRSystem.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public TicketsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Ticket/{token}
        [Route("Ticket/{token}")]
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrEmpty(token)) return NotFound();

            var guest = await _context.Guests
                .Include(g => g.Senior)
                .ThenInclude(s => s.Event)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.TicketToken == token);

            if (guest == null) return NotFound();

            return View(guest);
        }

        // GET: Ticket/QrImage/{token}
        [Route("Ticket/QrImage/{token}")]
        public async Task<IActionResult> QrImage(string token)
        {
            var guest = await _context.Guests
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.TicketToken == token);

            if (guest == null) return NotFound();

            // Link to Senior Details with highlightGuest parameter (Admin/Door view)
            var publicHost = _configuration["PublicHost"];
            var host = !string.IsNullOrWhiteSpace(publicHost) ? publicHost : $"{Request.Scheme}://{Request.Host}";
            
            // This URL is what the scanner (Admin/User) will open
            var url = $"{host}/Seniors/Details/{guest.SeniorId}?highlightGuest={guest.GuestId}&autoMark=true";

            using var generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrPng = new PngByteQRCode(data);
            var pngBytes = qrPng.GetGraphic(20);

            return File(pngBytes, "image/png", $"Ticket_{token}.png");
        }
    }
}
