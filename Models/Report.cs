using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("reports")]
public class Report
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("reporter_id")]
    public int ReporterId { get; set; }
    
    [Column("reported_message_id")]
    public int ReportedMessageId { get; set; }
    
    [Required]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;
    
    [Column("status")]
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("handled_by")]
    public int? HandledBy { get; set; }
    
    [Column("handled_at")]
    public DateTime? HandledAt { get; set; }
    
    [Column("admin_notes")]
    public string? AdminNotes { get; set; }
    
    // Navigation properties
    [ForeignKey("ReporterId")]
    public virtual User Reporter { get; set; } = null!;
    
    [ForeignKey("ReportedMessageId")]
    public virtual Message ReportedMessage { get; set; } = null!;
    
    [ForeignKey("HandledBy")]
    public virtual User? HandledByUser { get; set; }
}
