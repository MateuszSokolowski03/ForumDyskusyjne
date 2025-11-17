using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("admin_actions")]
public class AdminAction
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("admin_id")]
    public int AdminId { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    [Column("target_type")]
    public string TargetType { get; set; } = string.Empty;
    
    [Column("target_id")]
    public int? TargetId { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("AdminId")]
    public virtual User Admin { get; set; } = null!;
}
