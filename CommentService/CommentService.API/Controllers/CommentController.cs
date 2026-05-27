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
    private readonly RabbitMQPublisher _publisher;

    public CommentController(ICommentRepository commentRepository, RabbitMQPublisher publisher)
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
        var comments = await _commentRepository.GetByArtworkIdAsync(artworkId);
        var average = await _commentRepository.GetAverageRatingAsync(artworkId);

        return Ok(new ArtworkRatingDto
        {
            ArtworkId = artworkId,
            AverageRating = average,
            TotalComments = comments.Count()
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommentResponseDto>> Create(CreateCommentDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Puan 1-5 arasında olmalıdır.");

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Content = dto.Content,
            Rating = dto.Rating,
            ArtworkId = dto.ArtworkId,
            UserId = userId,
            Username = username,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.CreateAsync(comment);

        // RabbitMQ'ya event gönder
        _publisher.Publish(new
        {
            ArtistId = dto.ArtistId,
            Username = username,
            Content = dto.Content
        });

        return CreatedAtAction(nameof(GetByArtwork), new { artworkId = created.ArtworkId }, MapToDto(created));
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
        CreatedAt = comment.CreatedAt
    };
}