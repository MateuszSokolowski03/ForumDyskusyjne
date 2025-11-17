using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("attachment")]
public class Attachment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("message_id")]
    public int MessageId { get; set; }
    
    [Required]
    [StringLength(500)]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;
    
    [Column("file_size")]
    public int FileSize { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(255)]
    [Column("original_filename")]
    public string? OriginalFilename { get; set; }
    
    [StringLength(100)]
    [Column("mime_type")]
    public string? MimeType { get; set; }
    
    [Column("download_count")]
    public int DownloadCount { get; set; } = 0;
    
    // Navigation properties
    [ForeignKey("MessageId")]
    public virtual Message Message { get; set; } = null!;
}
