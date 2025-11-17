using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("private_messages")]
public class PrivateMessage
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("sender_id")]
    public int SenderId { get; set; }
    
    [Column("recipient_id")]
    public int RecipientId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("is_read")]
    public bool IsRead { get; set; } = false;
    
    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    [Column("deleted_by_sender")]
    public bool DeletedBySender { get; set; } = false;
    
    [Column("deleted_by_recipient")]
    public bool DeletedByRecipient { get; set; } = false;
    
    // Navigation properties
    [ForeignKey("SenderId")]
    public virtual User Sender { get; set; } = null!;
    
    [ForeignKey("RecipientId")]
    public virtual User Recipient { get; set; } = null!;
}
