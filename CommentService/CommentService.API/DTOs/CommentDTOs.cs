namespace CommentService.API.DTOs;

public class CreateCommentDto
{
    public string Content { get; set; } = string.Empty;

    // Required for reviews (parent), ignored for replies.
    public int? Rating { get; set; }
    public Guid ArtworkId { get; set; }

    public Guid ArtistId { get; set; }

    // NULL => create a review (parent). Otherwise create a reply to this parent.
    public Guid? ParentId { get; set; }
}

public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;

    // Applies only when editing a review (parent); ignored for replies.
    public int? Rating { get; set; }
}

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public Guid ArtworkId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? ParentId { get; set; }
    public List<CommentResponseDto> Replies { get; set; } = new();
}

public class ArtworkRatingDto
{
    public Guid ArtworkId { get; set; }
    public double AverageRating { get; set; }

    // Number of reviews (parent comments) backing the average.
    public int RatingCount { get; set; }
}

public class BatchArtworkRatingDto
{
    public Guid ArtworkId { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}
