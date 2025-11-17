using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumDyskusyjne.Models;

public enum UserRole
{
    User,
    Moderator, 
    Admin
}

[Table("user")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Column("role")]
    public UserRole Role { get; set; } = UserRole.User;
    
    [Column("avatar_url")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("last_activity_at")]
    public DateTime? LastActivityAt { get; set; }
    
    // Rozszerzenia z TODO.md
    [Column("email_verified")]
    public bool EmailVerified { get; set; } = false;
    
    [Column("bio")]
    public string? Bio { get; set; }
    
    [Column("last_login")]
    public DateTime? LastLogin { get; set; }
    
    [Column("is_banned")]
    public bool IsBanned { get; set; } = false;
    
    [Column("ban_reason")]
    public string? BanReason { get; set; }
    
    [Column("ban_expires_at")]
    public DateTime? BanExpiresAt { get; set; }
    
    [Column("login_attempts")]
    public int LoginAttempts { get; set; } = 0;
    
    [Column("last_ip_address")]
    [StringLength(45)]
    public string? LastIpAddress { get; set; }
    
    [Column("post_count")]
    public int PostCount { get; set; } = 0;
    
    [Column("current_rank_id")]
    public int? CurrentRankId { get; set; }
    
    [Column("auto_logout_minutes")]
    public int AutoLogoutMinutes { get; set; } = 30;
    
    [Column("messages_per_page")]
    public int MessagesPerPage { get; set; } = 20;
    
    [Column("threads_per_page")]
    public int ThreadsPerPage { get; set; } = 15;
    
    // Navigation properties
    [ForeignKey("CurrentRankId")]
    public virtual UserRank? CurrentRank { get; set; }
    
    public virtual ICollection<Thread> Threads { get; set; } = new List<Thread>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    public virtual ICollection<PrivateMessage> SentMessages { get; set; } = new List<PrivateMessage>();
    public virtual ICollection<PrivateMessage> ReceivedMessages { get; set; } = new List<PrivateMessage>();
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    public virtual ICollection<Report> HandledReports { get; set; } = new List<Report>();
    public virtual ICollection<BannedWord> BannedWords { get; set; } = new List<BannedWord>();
    public virtual ICollection<ForumModerator> ModeratedForums { get; set; } = new List<ForumModerator>();
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public virtual ICollection<UserRankHistory> RankHistory { get; set; } = new List<UserRankHistory>();
    public virtual ICollection<ContentModerationLog> ContentModerationLogs { get; set; } = new List<ContentModerationLog>();
    public virtual ICollection<AdminAction> AdminActions { get; set; } = new List<AdminAction>();
}
