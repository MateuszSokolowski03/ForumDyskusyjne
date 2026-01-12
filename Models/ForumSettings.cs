namespace ForumDyskusyjne.Models;

public class ForumSettings
{
    public int Id { get; set; }
    public string ForumName { get; set; } = "Forum Dyskusyjne";
    public string? LogoUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
