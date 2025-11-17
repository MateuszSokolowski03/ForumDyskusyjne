using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("user_rank_history")]
public class UserRankHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("old_rank_id")]
    public int? OldRankId { get; set; }
    
    [Column("new_rank_id")]
    public int? NewRankId { get; set; }
    
    [Column("post_count_at_change")]
    public int? PostCountAtChange { get; set; }
    
    [Column("change_reason")]
    public ChangeReason ChangeReason { get; set; } = ChangeReason.Automatic;
    
    [Column("changed_by")]
    public int? ChangedBy { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("OldRankId")]
    public virtual UserRank? OldRank { get; set; }
    
    [ForeignKey("NewRankId")]
    public virtual UserRank? NewRank { get; set; }
    
    [ForeignKey("ChangedBy")]
    public virtual User? ChangedByUser { get; set; }
}
