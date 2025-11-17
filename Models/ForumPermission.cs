using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("forum_permission")]
public class ForumPermission
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("forum_id")]
    public int ForumId { get; set; }
    
    [Column("can_read")]
    public bool CanRead { get; set; } = true;
    
    [Column("can_write")]
    public bool CanWrite { get; set; } = true;
    
    [Column("allow_anonymous_view")]
    public bool AllowAnonymousView { get; set; } = true;
    
    [Column("allow_anonymous_post")]
    public bool AllowAnonymousPost { get; set; } = false;
    
    // Navigation properties
    [ForeignKey("ForumId")]
    public virtual Forum Forum { get; set; } = null!;
}
