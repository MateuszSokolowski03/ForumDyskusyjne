using Microsoft.EntityFrameworkCore;
using ForumDyskusyjne.Models;

namespace ForumDyskusyjne.Data;

public class ForumDbContext : DbContext
{
    public ForumDbContext(DbContextOptions<ForumDbContext> options) : base(options)
    {
    }

    // DbSets dla wszystkich tabel
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Forum> Forums { get; set; }
    public DbSet<Models.Thread> Threads { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<UserRank> UserRanks { get; set; }
    public DbSet<ForumModerator> ForumModerators { get; set; }
    public DbSet<ForumPermission> ForumPermissions { get; set; }
    public DbSet<BannedWord> BannedWords { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<PrivateMessage> PrivateMessages { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<UserRankHistory> UserRankHistories { get; set; }
    public DbSet<ContentModerationLog> ContentModerationLogs { get; set; }
    public DbSet<AdminAction> AdminActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapowanie nazw tabel (PostgreSQL używa snake_case)
        modelBuilder.Entity<User>().ToTable("user");
        modelBuilder.Entity<Category>().ToTable("category");
        modelBuilder.Entity<Forum>().ToTable("forum");
        modelBuilder.Entity<Models.Thread>().ToTable("thread");
        modelBuilder.Entity<Message>().ToTable("message");
        modelBuilder.Entity<Attachment>().ToTable("attachment");
        modelBuilder.Entity<UserRank>().ToTable("user_rank");
        modelBuilder.Entity<ForumModerator>().ToTable("forum_moderator");
        modelBuilder.Entity<ForumPermission>().ToTable("forum_permission");
        modelBuilder.Entity<BannedWord>().ToTable("banned_word");
        modelBuilder.Entity<Announcement>().ToTable("announcements");
        modelBuilder.Entity<PrivateMessage>().ToTable("private_messages");
        modelBuilder.Entity<Report>().ToTable("reports");
        modelBuilder.Entity<PasswordResetToken>().ToTable("password_reset_tokens");
        modelBuilder.Entity<UserRankHistory>().ToTable("user_rank_history");
        modelBuilder.Entity<ContentModerationLog>().ToTable("content_moderation_logs");
        modelBuilder.Entity<AdminAction>().ToTable("admin_actions");

        // Konfiguracja kluczy głównych złożonych
        modelBuilder.Entity<ForumModerator>()
            .HasKey(fm => new { fm.ForumId, fm.UserId });

        // Konfiguracja enum-ów dla PostgreSQL
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<BannedWord>()
            .Property(bw => bw.SeverityLevel)
            .HasConversion<string>();

        modelBuilder.Entity<BannedWord>()
            .Property(bw => bw.MatchType)
            .HasConversion<string>();

        modelBuilder.Entity<Report>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<UserRankHistory>()
            .Property(urh => urh.ChangeReason)
            .HasConversion<string>();

        modelBuilder.Entity<ContentModerationLog>()
            .Property(cml => cml.ActionTaken)
            .HasConversion<string>();

        // Konfiguracja relacji many-to-many i self-referencing
        modelBuilder.Entity<ForumModerator>()
            .HasOne(fm => fm.AssignedByUser)
            .WithMany()
            .HasForeignKey(fm => fm.AssignedBy)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.SetNull);

        modelBuilder.Entity<PrivateMessage>()
            .HasOne(pm => pm.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(pm => pm.SenderId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        modelBuilder.Entity<PrivateMessage>()
            .HasOne(pm => pm.Recipient)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(pm => pm.RecipientId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Reporter)
            .WithMany(u => u.Reports)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.HandledByUser)
            .WithMany(u => u.HandledReports)
            .HasForeignKey(r => r.HandledBy)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.SetNull);

        // Konfiguracja relacji dla UserRankHistory
        modelBuilder.Entity<UserRankHistory>()
            .HasOne(urh => urh.User)
            .WithMany(u => u.RankHistory)
            .HasForeignKey(urh => urh.UserId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRankHistory>()
            .HasOne(urh => urh.ChangedByUser)
            .WithMany()
            .HasForeignKey(urh => urh.ChangedBy)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.SetNull);

        modelBuilder.Entity<UserRankHistory>()
            .HasOne(urh => urh.OldRank)
            .WithMany()
            .HasForeignKey(urh => urh.OldRankId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.SetNull);

        modelBuilder.Entity<UserRankHistory>()
            .HasOne(urh => urh.NewRank)
            .WithMany(ur => ur.RankHistories)
            .HasForeignKey(urh => urh.NewRankId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.SetNull);

        base.OnModelCreating(modelBuilder);
    }
}
