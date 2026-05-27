using ArtService.API.DTOs;
using ArtService.Core.Entities;
using ArtService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArtService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtworkController : ControllerBase
{
    private readonly IArtworkRepository _artworkRepository;

    public ArtworkController(IArtworkRepository artworkRepository)
    {
        _artworkRepository = artworkRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArtworkResponseDto>>> GetAll(
        [FromQuery] string? category, [FromQuery] string? keyword)
    {
        var artworks = await _artworkRepository.SearchAsync(category, keyword);
        return Ok(artworks.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ArtworkResponseDto>> GetById(Guid id)
    {
        var artwork = await _artworkRepository.GetByIdAsync(id);
        if (artwork == null) return NotFound();
        return Ok(MapToDto(artwork));
    }

    [HttpGet("artist/{artistId}")]
    public async Task<ActionResult<IEnumerable<ArtworkResponseDto>>> GetByArtist(Guid artistId)
    {
        var artworks = await _artworkRepository.GetByArtistIdAsync(artistId);
        return Ok(artworks.Select(MapToDto));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ArtworkResponseDto>> Create(CreateArtworkDto dto)
    {
        var artistId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var artistUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var artwork = new Artwork
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Category = dto.Category,
            ArtistId = artistId,
            ArtistUsername = artistUsername,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _artworkRepository.CreateAsync(artwork);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ArtworkResponseDto>> Update(Guid id, UpdateArtworkDto dto)
    {
        var artwork = await _artworkRepository.GetByIdAsync(id);
        if (artwork == null) return NotFound();

        var artistId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (artwork.ArtistId != artistId) return Forbid();

        if (dto.Title != null) artwork.Title = dto.Title;
        if (dto.Description != null) artwork.Description = dto.Description;
        if (dto.ImageUrl != null) artwork.ImageUrl = dto.ImageUrl;
        if (dto.Category != null) artwork.Category = dto.Category;

        var updated = await _artworkRepository.UpdateAsync(artwork);
        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var artwork = await _artworkRepository.GetByIdAsync(id);
        if (artwork == null) return NotFound();

        var artistId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (artwork.ArtistId != artistId) return Forbid();

        await _artworkRepository.DeleteAsync(id);
        return NoContent();
    }

    private ArtworkResponseDto MapToDto(Artwork artwork) => new()
    {
        Id = artwork.Id,
        Title = artwork.Title,
        Description = artwork.Description,
        ImageUrl = artwork.ImageUrl,
        Category = artwork.Category,
        ArtistId = artwork.ArtistId,
        ArtistUsername = artwork.ArtistUsername,
        CreatedAt = artwork.CreatedAt
    };
}