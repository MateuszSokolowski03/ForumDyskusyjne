using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("message")]
public class Message
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("thread_id")]
    public int ThreadId { get; set; }
    
    [Column("author_id")]
    public int AuthorId { get; set; }
    
    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("is_edited")]
    public bool IsEdited { get; set; } = false;
    
    [Column("edited_at")]
    public DateTime? EditedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("ThreadId")]
    public virtual Thread Thread { get; set; } = null!;
    
    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;
    
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
