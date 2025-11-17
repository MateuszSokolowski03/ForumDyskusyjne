using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("forum")]
public class Forum
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("category_id")]
    public int CategoryId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = null!;
    
    public virtual ICollection<Thread> Threads { get; set; } = new List<Thread>();
    public virtual ICollection<ForumModerator> ForumModerators { get; set; } = new List<ForumModerator>();
    public virtual ICollection<ForumPermission> ForumPermissions { get; set; } = new List<ForumPermission>();
}
