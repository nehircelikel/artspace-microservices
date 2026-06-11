namespace CommentService.Core.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;

    // Parent comments (reviews) always carry a rating; replies leave this NULL.
    public int? Rating { get; set; }

    public Guid ArtworkId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Set the first time the comment is edited; NULL means never edited.
    public DateTime? UpdatedAt { get; set; }

    // NULL => parent (review). Otherwise points at the parent comment this reply
    // belongs to. Deleting a parent cascades to its replies.
    public Guid? ParentId { get; set; }
    public Comment? Parent { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
