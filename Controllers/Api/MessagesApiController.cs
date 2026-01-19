using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;
using System.Security.Claims;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/messages")]
    [ApiController]
    [Authorize]
    public class MessagesApiController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public MessagesApiController(ForumDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            throw new UnauthorizedAccessException("Użytkownik nie jest zalogowany");
        }
        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllMessages()
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Author)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();
                
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var senderId = GetCurrentUserId();

                Console.WriteLine($"SendMessage called - SenderId: {senderId}, Request: {System.Text.Json.JsonSerializer.Serialize(request)}");

                if (request == null)
                {
                    Console.WriteLine("ERROR: Request is null");
                    return BadRequest(new { error = "Brak danych żądania" });
                }

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    Console.WriteLine($"ERROR: Content is empty. Content='{request.Content}'");
                    return BadRequest(new { error = "Treść wiadomości jest wymagana" });
                }

                int? recipientId = request.RecipientId > 0 ? request.RecipientId : (int?)null;
                Console.WriteLine($"RecipientId from request: {request.RecipientId}, Parsed: {recipientId}");
                Console.WriteLine($"RecipientUsername from request: '{request.RecipientUsername}'");
                
                if (recipientId == null)
                {
                    if (string.IsNullOrWhiteSpace(request.RecipientUsername))
                    {
                        Console.WriteLine($"ERROR: Both RecipientId and RecipientUsername are missing");
                        return BadRequest(new { error = "RecipientId lub RecipientUsername wymagane" });
                    }

                    var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.RecipientUsername);
                    if (recipient == null)
                    {
                        Console.WriteLine($"ERROR: Recipient not found for username: {request.RecipientUsername}");
                        return NotFound(new { error = "Odbiorca nie znaleziony" });
                    }

                    recipientId = recipient.Id;
                    Console.WriteLine($"Found recipient ID: {recipientId}");
                }

                if (recipientId == senderId)
                    return BadRequest(new { error = "Nie możesz wysłać wiadomości do siebie" });

                var pm = new PrivateMessage
                {
                    SenderId = senderId,
                    RecipientId = recipientId.Value,
                    Subject = request.Subject ?? string.Empty,
                    Content = request.Content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    DeletedBySender = false,
                    DeletedByRecipient = false
                };

                _context.PrivateMessages.Add(pm);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMessage), new { id = pm.Id }, new { id = pm.Id, message = "Wiadomość wysłana" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("inbox")]
        public async Task<IActionResult> GetInbox()
        {
            try
            {
                var userId = GetCurrentUserId();

                var messages = await _context.PrivateMessages
                    .Where(pm => pm.RecipientId == userId && !pm.DeletedByRecipient)
                    .Include(pm => pm.Sender)
                    .OrderByDescending(pm => pm.SentAt)
                    .Select(pm => new
                    {
                        pm.Id,
                        pm.Subject,
                        pm.Content,
                        pm.IsRead,
                        pm.SentAt,
                        senderId = pm.Sender.Id,
                        senderName = pm.Sender.Username,
                        Sender = new { pm.Sender.Id, pm.Sender.Username }
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("sent")]
        public async Task<IActionResult> GetSent()
        {
            try
            {
                var userId = GetCurrentUserId();

                var messages = await _context.PrivateMessages
                    .Where(pm => pm.SenderId == userId && !pm.DeletedBySender)
                    .Include(pm => pm.Recipient)
                    .OrderByDescending(pm => pm.SentAt)
                    .Select(pm => new
                    {
                        pm.Id,
                        pm.Subject,
                        pm.Content,
                        pm.SentAt,
                        recipientId = pm.Recipient.Id,
                        recipientName = pm.Recipient.Username,
                        Recipient = new { pm.Recipient.Id, pm.Recipient.Username }
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessage(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var message = await _context.PrivateMessages
                    .Include(pm => pm.Sender)
                    .Include(pm => pm.Recipient)
                    .FirstOrDefaultAsync(pm => pm.Id == id);

                if (message == null)
                    return NotFound(new { error = "Wiadomość nie znaleziona" });

                if (message.SenderId != userId && message.RecipientId != userId)
                    return Forbid();

                if ((message.SenderId == userId && message.DeletedBySender) ||
                    (message.RecipientId == userId && message.DeletedByRecipient))
                    return NotFound(new { error = "Wiadomość została usunięta" });

                if (message.RecipientId == userId && !message.IsRead)
                {
                    message.IsRead = true;
                    _context.PrivateMessages.Update(message);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message.Id,
                    message.Subject,
                    message.Content,
                    message.IsRead,
                    message.SentAt,
                    message.SenderId,
                    message.RecipientId,
                    SenderName = message.Sender.Username,
                    Sender = new { message.Sender.Id, message.Sender.Username },
                    RecipientName = message.Recipient.Username,
                    Recipient = new { message.Recipient.Id, message.Recipient.Username }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var message = await _context.PrivateMessages.FindAsync(id);
                if (message == null)
                    return NotFound(new { error = "Wiadomość nie znaleziona" });

                if (message.RecipientId != userId)
                    return Forbid();

                message.IsRead = true;
                _context.PrivateMessages.Update(message);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wiadomość oznaczona jako przeczytana" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var message = await _context.PrivateMessages.FindAsync(id);
                if (message == null)
                    return NotFound(new { error = "Wiadomość nie znaleziona" });

                if (message.SenderId != userId && message.RecipientId != userId)
                    return Forbid();

                if (message.SenderId == userId)
                    message.DeletedBySender = true;

                if (message.RecipientId == userId)
                    message.DeletedByRecipient = true;

                if (message.DeletedBySender && message.DeletedByRecipient)
                    _context.PrivateMessages.Remove(message);
                else
                    _context.PrivateMessages.Update(message);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Wiadomość usunięta" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();

                var unreadCount = await _context.PrivateMessages
                    .Where(pm => pm.RecipientId == userId && !pm.IsRead && !pm.DeletedByRecipient)
                    .CountAsync();

                return Ok(new { unreadCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("search-users")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                    return BadRequest(new { error = "Zapytanie musi mieć co najmniej 2 znaki" });

                var users = await _context.Users
                    .Where(u => u.Username.Contains(query) && !u.IsBanned)
                    .Take(10)
                    .Select(u => new { u.Id, u.Username })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SendMessageRequest
    {
        public int RecipientId { get; set; } = 0;
        public string RecipientUsername { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}