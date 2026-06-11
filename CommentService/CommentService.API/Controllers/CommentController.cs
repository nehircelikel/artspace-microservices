using CommentService.API.DTOs;
using CommentService.Core.Entities;
using CommentService.Core.Interfaces;
using CommentService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentController : ControllerBase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IRabbitMQPublisher _publisher;

    public CommentController(ICommentRepository commentRepository, IRabbitMQPublisher publisher)
    {
        _commentRepository = commentRepository;
        _publisher = publisher;
    }

    [HttpGet("artwork/{artworkId}")]
    public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetByArtwork(Guid artworkId)
    {
        var comments = await _commentRepository.GetByArtworkIdAsync(artworkId);
        return Ok(comments.Select(MapToDto));
    }

    [HttpGet("artwork/{artworkId}/rating")]
    public async Task<ActionResult<ArtworkRatingDto>> GetRating(Guid artworkId)
    {
        var average = await _commentRepository.GetAverageRatingAsync(artworkId);
        var count = await _commentRepository.GetRatingCountAsync(artworkId);

        return Ok(new ArtworkRatingDto
        {
            ArtworkId = artworkId,
            AverageRating = average,
            RatingCount = count
        });
    }

    // Batch ratings for the listing page: GET /api/Comment/ratings?artworkIds=a,b,c
    [HttpGet("ratings")]
    public async Task<ActionResult<IEnumerable<BatchArtworkRatingDto>>> GetRatings([FromQuery] string? artworkIds)
    {
        if (string.IsNullOrWhiteSpace(artworkIds))
            return Ok(Enumerable.Empty<BatchArtworkRatingDto>());

        var ids = artworkIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        var ratings = await _commentRepository.GetRatingsForArtworksAsync(ids);

        return Ok(ratings.Select(r => new BatchArtworkRatingDto
        {
            ArtworkId = r.ArtworkId,
            AverageRating = r.AverageRating,
            RatingCount = r.RatingCount
        }));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommentResponseDto>> Create(CreateCommentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        int? rating;
        Guid recipientId;   // who gets notified about this comment
        bool isReply;

        if (dto.ParentId.HasValue)
        {
            // Reply: must hang off an existing comment, carries no rating.
            // The person being replied to is notified — not the artwork's artist.
            var parent = await _commentRepository.GetByIdAsync(dto.ParentId.Value);
            if (parent == null) return NotFound("The comment you are replying to was not found.");
            rating = null;
            recipientId = parent.UserId;
            isReply = true;
        }
        else
        {
            // Review: rating required, and the artist cannot review their own work.
            if (dto.Rating is not (>= 1 and <= 5))
                return BadRequest("Rating must be between 1 and 5.");

            if (dto.ArtistId == userId)
                return Forbid();

            // One review per user per artwork.
            if (await _commentRepository.HasUserReviewedAsync(dto.ArtworkId, userId))
                return Conflict("You have already reviewed this artwork.");

            rating = dto.Rating;
            recipientId = dto.ArtistId;   // the artwork's artist is notified
            isReply = false;
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Content = dto.Content,
            Rating = rating,
            ArtworkId = dto.ArtworkId,
            UserId = userId,
            Username = username,
            ParentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.CreateAsync(comment);

        // Notify the recipient via RabbitMQ — but never notify someone about their
        // own comment (e.g. an artist replying to a review on their own artwork).
        if (recipientId != Guid.Empty && recipientId != userId)
        {
            _publisher.Publish(new
            {
                RecipientId = recipientId,
                Username = username,
                Content = dto.Content,
                IsReply = isReply
            });
        }

        return CreatedAtAction(nameof(GetByArtwork), new { artworkId = created.ArtworkId }, MapToDto(created));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<CommentResponseDto>> Update(Guid id, UpdateCommentDto dto)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        if (comment == null) return NotFound();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (comment.UserId != userId) return Forbid();

        comment.Content = dto.Content;

        // Only reviews (parents) carry an editable rating.
        if (comment.ParentId == null)
        {
            if (dto.Rating is not (>= 1 and <= 5))
                return BadRequest("Rating must be between 1 and 5.");
            comment.Rating = dto.Rating;
        }

        comment.UpdatedAt = DateTime.UtcNow;

        var updated = await _commentRepository.UpdateAsync(comment);
        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        if (comment == null) return NotFound();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (comment.UserId != userId) return Forbid();

        await _commentRepository.DeleteAsync(id);
        return NoContent();
    }

    private CommentResponseDto MapToDto(Comment comment) => new()
    {
        Id = comment.Id,
        Content = comment.Content,
        Rating = comment.Rating,
        ArtworkId = comment.ArtworkId,
        UserId = comment.UserId,
        Username = comment.Username,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt,
        ParentId = comment.ParentId,
        Replies = comment.Replies
            .OrderBy(r => r.CreatedAt)
            .Select(MapToDto)
            .ToList()
    };
}
