namespace CommentService.Core.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } 
    public Guid ArtworkId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}