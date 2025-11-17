using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("user_rank")]
public class UserRank
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("min_messages")]
    public int MinMessages { get; set; } = 0;
    
    [Column("can_be_set_manually")]
    public bool CanBeSetManually { get; set; } = false;
    
    [Column("max_messages")]
    public int? MaxMessages { get; set; }
    
    [StringLength(7)]
    [Column("color")]
    public string? Color { get; set; }
    
    [StringLength(50)]
    [Column("icon")]
    public string? Icon { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<UserRankHistory> RankHistories { get; set; } = new List<UserRankHistory>();
}
