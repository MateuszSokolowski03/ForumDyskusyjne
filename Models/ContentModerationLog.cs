using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("content_moderation_logs")]
public class ContentModerationLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("message_id")]
    public int? MessageId { get; set; }
    
    [Column("banned_word_id")]
    public int? BannedWordId { get; set; }
    
    [Column("detected_text")]
    public string? DetectedText { get; set; }
    
    [Column("action_taken")]
    public ActionTaken ActionTaken { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("MessageId")]
    public virtual Message? Message { get; set; }
    
    [ForeignKey("BannedWordId")]
    public virtual BannedWord? BannedWord { get; set; }
}
