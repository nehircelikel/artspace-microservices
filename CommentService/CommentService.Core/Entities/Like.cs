namespace CommentService.Core.Entities;

public class Like
{
    public Guid Id { get; set; }
    public Guid ArtworkId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
