using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("thread")]
public class Thread
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("author_id")]
    public int AuthorId { get; set; }
    
    [Column("forum_id")]
    public int ForumId { get; set; }
    
    [Column("is_pinned")]
    public bool IsPinned { get; set; } = false;
    
    [Column("views")]
    public int Views { get; set; } = 0;
    
    [Column("replies_count")]
    public int RepliesCount { get; set; } = 0;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("AuthorId")]
    public virtual User Author { get; set; } = null!;
    
    [ForeignKey("ForumId")]
    public virtual Forum Forum { get; set; } = null!;
    
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
