namespace CommentService.API.DTOs;

public class CreateCommentDto
{
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public Guid ArtworkId { get; set; }

    public Guid ArtistId { get; set; }
}

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public Guid ArtworkId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ArtworkRatingDto
{
    public Guid ArtworkId { get; set; }
    public double AverageRating { get; set; }
    public int TotalComments { get; set; }
}