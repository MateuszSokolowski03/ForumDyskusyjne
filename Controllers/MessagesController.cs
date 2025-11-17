using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Data;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ForumDbContext _context;

        public MessagesController(ForumDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetMessages(int userId, string type = "received", int page = 1, int pageSize = 20)
        {
            try
            {
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
                var query = _context.PrivateMessages
                    .Include(pm => pm.Sender)
                    .Include(pm => pm.Recipient)
                    .AsQueryable();

                // Filtruj według typu
                switch (type.ToLower())
                {
                    case "sent":
                        query = query.Where(pm => pm.SenderId == userId && !pm.DeletedBySender);
                        break;
                    case "received":
                    default:
                        query = query.Where(pm => pm.RecipientId == userId && !pm.DeletedByRecipient);
                        break;
                }

                var totalMessages = await query.CountAsync();
                var messages = await query
                    .OrderByDescending(pm => pm.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(pm => new
                    {
                        pm.Id,
                        pm.Subject,
                        pm.IsRead,
                        pm.SentAt,
                        Sender = new
                        {
                            pm.Sender.Id,
                            pm.Sender.Username,
                            pm.Sender.AvatarUrl
                        },
                        Recipient = new
                        {
                            pm.Recipient.Id,
                            pm.Recipient.Username,
                            pm.Recipient.AvatarUrl
                        },
                        // Podgląd treści (pierwsze 100 znaków)
                        ContentPreview = pm.Content.Length > 100 
                            ? pm.Content.Substring(0, 100) + "..." 
                            : pm.Content
                    })
                    .ToListAsync();

                return Ok(new
                {
                    messages,
                    totalMessages,
                    totalPages = (int)Math.Ceiling((double)totalMessages / pageSize),
                    currentPage = page,
                    pageSize,
                    type
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessage(int id, int userId)
        {
            try
            {
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
                var message = await _context.PrivateMessages
                    .Include(pm => pm.Sender)
                    .Include(pm => pm.Recipient)
                    .FirstOrDefaultAsync(pm => pm.Id == id);

                if (message == null)
                {
                    return NotFound(new { error = "Wiadomość nie została znaleziona" });
                }

                // Sprawdź czy użytkownik ma prawo do odczytania wiadomości
                if (message.SenderId != userId && message.RecipientId != userId)
                {
                    return Forbid("Nie masz uprawnień do odczytania tej wiadomości");
                }

                // Sprawdź czy wiadomość nie została usunięta
                if ((message.SenderId == userId && message.DeletedBySender) ||
                    (message.RecipientId == userId && message.DeletedByRecipient))
                {
                    return NotFound(new { error = "Wiadomość została usunięta" });
                }

                // Oznacz jako przeczytaną jeśli jest odbiorcą
                if (message.RecipientId == userId && !message.IsRead)
                {
                    message.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message.Id,
                    message.Subject,
                    message.Content,
                    message.IsRead,
                    message.SentAt,
                    Sender = new
                    {
                        message.Sender.Id,
                        message.Sender.Username,
                        message.Sender.AvatarUrl
                    },
                    Recipient = new
                    {
                        message.Recipient.Id,
                        message.Recipient.Username,
                        message.Recipient.AvatarUrl
                    }
                });
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
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
                if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { error = "Temat i treść wiadomości są wymagane" });
                }

                if (request.SenderId == request.RecipientId)
                {
                    return BadRequest(new { error = "Nie możesz wysłać wiadomości do siebie" });
                }

                // Sprawdź czy odbiorca istnieje
                var recipient = await _context.Users.FindAsync(request.RecipientId);
                if (recipient == null)
                {
                    return NotFound(new { error = "Odbiorca nie został znaleziony" });
                }

                // Sprawdź czy nadawca istnieje
                var sender = await _context.Users.FindAsync(request.SenderId);
                if (sender == null)
                {
                    return NotFound(new { error = "Nadawca nie został znaleziony" });
                }

                var privateMessage = new PrivateMessage
                {
                    SenderId = request.SenderId,
                    RecipientId = request.RecipientId,
                    Subject = request.Subject.Trim(),
                    Content = request.Content.Trim(),
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.PrivateMessages.Add(privateMessage);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Wiadomość została wysłana",
                    messageId = privateMessage.Id,
                    sentAt = privateMessage.SentAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id, int userId)
        {
            try
            {
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
                var message = await _context.PrivateMessages.FindAsync(id);
                if (message == null)
                {
                    return NotFound(new { error = "Wiadomość nie została znaleziona" });
                }

                // Sprawdź czy użytkownik jest odbiorcą
                if (message.RecipientId != userId)
                {
                    return Forbid("Nie masz uprawnień do oznaczenia tej wiadomości jako przeczytanej");
                }

                message.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wiadomość została oznaczona jako przeczytana" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            try
            {
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
                var message = await _context.PrivateMessages.FindAsync(id);
                if (message == null)
                {
                    return NotFound(new { error = "Wiadomość nie została znaleziona" });
                }

                // Sprawdź czy użytkownik ma prawo do usunięcia
                if (message.SenderId != userId && message.RecipientId != userId)
                {
                    return Forbid("Nie masz uprawnień do usunięcia tej wiadomości");
                }

                // Oznacz jako usuniętą dla odpowiedniego użytkownika
                if (message.SenderId == userId)
                {
                    message.DeletedBySender = true;
                }
                
                if (message.RecipientId == userId)
                {
                    message.DeletedByRecipient = true;
                }

                // Jeśli usunięta przez obu użytkowników, usuń fizycznie z bazy
                if (message.DeletedBySender && message.DeletedByRecipient)
                {
                    _context.PrivateMessages.Remove(message);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Wiadomość została usunięta" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            try
            {
                // TODO: Walidacja użytkownika (po implementacji autentyfikacji)
                
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
        public async Task<IActionResult> SearchUsers(string query, int currentUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest(new { error = "Zapytanie musi mieć co najmniej 2 znaki" });
                }

                var users = await _context.Users
                    .Where(u => u.Id != currentUserId && 
                               (u.Username.Contains(query) || u.Email.Contains(query)) &&
                               !u.IsBanned)
                    .Take(10)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.AvatarUrl
                    })
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
        public int SenderId { get; set; } // Tymczasowo, później z sesji/tokena
        public int RecipientId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
