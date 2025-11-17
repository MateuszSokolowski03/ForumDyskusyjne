using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("banned_word")]
public class BannedWord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    [Column("word")]
    public string Word { get; set; } = string.Empty;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("severity_level")]
    public SeverityLevel SeverityLevel { get; set; } = SeverityLevel.Warning;
    
    [Column("match_type")]
    public MatchType MatchType { get; set; } = MatchType.Contains;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_by")]
    public int? CreatedBy { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("usage_count")]
    public int UsageCount { get; set; } = 0;
    
    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }
    
    public virtual ICollection<ContentModerationLog> ContentModerationLogs { get; set; } = new List<ContentModerationLog>();
}
