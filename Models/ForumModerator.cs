using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("forum_moderator")]
public class ForumModerator
{
    [Column("forum_id")]
    public int ForumId { get; set; }
    
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("assigned_by")]
    public int? AssignedBy { get; set; }
    
    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("ForumId")]
    public virtual Forum Forum { get; set; } = null!;
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("AssignedBy")]
    public virtual User? AssignedByUser { get; set; }
}
