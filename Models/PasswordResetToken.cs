using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

[Table("password_reset_tokens")]
public class PasswordResetToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    public int UserId { get; set; }
    
    [Required]
    [StringLength(255)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;
    
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
    
    [Column("used_at")]
    public DateTime? UsedAt { get; set; }
    
    [StringLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }
    
    [Column("user_agent")]
    public string? UserAgent { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("is_used")]
    public bool IsUsed { get; set; } = false;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
