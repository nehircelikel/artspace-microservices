namespace ArtService.API.DTOs;

public class CreateArtworkDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class UpdateArtworkDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
}

public class ArtworkResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string ArtistUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}