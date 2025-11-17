using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("announcements")]
public class Announcement
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("created_by")]
    public int CreatedBy { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;
}
