using CommentService.API.DTOs;
using CommentService.Core.Entities;
using CommentService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommentService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LikeController : ControllerBase
{
    private readonly ILikeRepository _likeRepository;

    public LikeController(ILikeRepository likeRepository)
    {
        _likeRepository = likeRepository;
    }

    // Toggle like/save: if already liked, remove it; otherwise create it.
    [HttpPost("artwork/{artworkId}")]
    [Authorize]
    public async Task<ActionResult<LikeStatusDto>> Toggle(Guid artworkId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var existing = await _likeRepository.GetByArtworkAndUserAsync(artworkId, userId);

        bool isLiked;
        if (existing != null)
        {
            await _likeRepository.DeleteAsync(existing.Id);
            isLiked = false;
        }
        else
        {
            await _likeRepository.CreateAsync(new Like
            {
                Id = Guid.NewGuid(),
                ArtworkId = artworkId,
                UserId = userId,
                Username = username,
                CreatedAt = DateTime.UtcNow
            });
            isLiked = true;
        }

        var total = await _likeRepository.GetCountByArtworkIdAsync(artworkId);

        return Ok(new LikeStatusDto
        {
            ArtworkId = artworkId,
            IsLiked = isLiked,
            TotalLikes = total
        });
    }

    // Public: total like count for an artwork.
    [HttpGet("artwork/{artworkId}/count")]
    public async Task<ActionResult<LikeStatusDto>> GetCount(Guid artworkId)
    {
        var total = await _likeRepository.GetCountByArtworkIdAsync(artworkId);
        return Ok(new LikeStatusDto
        {
            ArtworkId = artworkId,
            IsLiked = false,
            TotalLikes = total
        });
    }

    // Whether the current user has liked/saved a given artwork.
    [HttpGet("artwork/{artworkId}/status")]
    [Authorize]
    public async Task<ActionResult<LikeStatusDto>> GetStatus(Guid artworkId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var existing = await _likeRepository.GetByArtworkAndUserAsync(artworkId, userId);
        var total = await _likeRepository.GetCountByArtworkIdAsync(artworkId);

        return Ok(new LikeStatusDto
        {
            ArtworkId = artworkId,
            IsLiked = existing != null,
            TotalLikes = total
        });
    }

    // The current user's saved/liked artworks.
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<LikedArtworkDto>>> GetMyLikes()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var likes = await _likeRepository.GetByUserIdAsync(userId);

        return Ok(likes.Select(l => new LikedArtworkDto
        {
            Id = l.Id,
            ArtworkId = l.ArtworkId,
            UserId = l.UserId,
            Username = l.Username,
            CreatedAt = l.CreatedAt
        }));
    }
}
