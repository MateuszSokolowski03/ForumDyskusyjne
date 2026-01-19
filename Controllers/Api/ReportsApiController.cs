using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Controllers.Api
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public ReportsApiController(ForumDbContext context)
        {
            _context = context;
        }

        // GET: api/reports
        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetReports()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            bool isAdmin = User.IsInRole("Admin");

            var query = _context.Reports
                .Include(r => r.ReportedMessage)
                    .ThenInclude(m => m.Thread)
                .Include(r => r.Reporter)
                .AsQueryable();

            if (!isAdmin)
            {
                // ✅ POPRAWKA: Używamy poprawnej nazwy tabeli: ForumModerators
                var myForumIds = await _context.ForumModerators
                    .Where(fm => fm.UserId == userId)
                    .Select(fm => fm.ForumId)
                    .ToListAsync();

                if (!myForumIds.Any())
                {
                    return Ok(new List<object>());
                }

                query = query.Where(r => 
                    r.ReportedMessage != null && 
                    r.ReportedMessage.Thread != null && 
                    myForumIds.Contains(r.ReportedMessage.Thread.ForumId));
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.CreatedAt,
                    r.Reason,
                    r.Status,
                    r.AdminNotes,
                    Reporter = new { Username = r.Reporter.Username },
                    ReportedMessageId = r.ReportedMessageId,
                    ReportedMessage = r.ReportedMessage == null ? null : new 
                    { 
                        Content = r.ReportedMessage.Content,
                        ThreadId = r.ReportedMessage.ThreadId
                    }
                })
                .ToListAsync();

            return Ok(reports);
        }

        // POST: api/reports
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var message = await _context.Messages.FindAsync(dto.MessageId);
            if (message == null)
            {
                return NotFound(new { error = "Wiadomość nie istnieje." });
            }

            bool alreadyReported = await _context.Reports.AnyAsync(r =>
                r.ReportedMessageId == dto.MessageId &&
                r.ReporterId == userId &&
                (r.Status == ReportStatus.Pending));

            if (alreadyReported)
            {
                return BadRequest(new { error = "Już zgłosiłeś tę wiadomość." });
            }

            var report = new Report
            {
                ReportedMessageId = dto.MessageId,
                ReporterId = userId,
                Reason = dto.Reason,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Zgłoszenie wysłane pomyślnie." });
        }

        // PUT: api/reports/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateReportDto dto)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound(new { error = "Zgłoszenie nie istnieje." });

            report.Status = dto.Status;
            report.AdminNotes = dto.AdminNotes;

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int adminId))
            {
                report.HandledBy = adminId;
                report.HandledAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Status zgłoszenia zaktualizowany." });
        }

        // DTOs - Poprawione ostrzeżenia CS8618
        public class CreateReportDto
        {
            public int MessageId { get; set; }
            public required string Reason { get; set; } // Wymagane pole
        }

        public class UpdateReportDto
        {
            public int Id { get; set; }
            public ReportStatus Status { get; set; }
            public string? AdminNotes { get; set; } // Może być null
        }
    }
}