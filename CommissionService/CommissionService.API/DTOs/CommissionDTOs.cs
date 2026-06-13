namespace CommissionService.API.DTOs;

public class CreateCommissionDto
{
    public Guid ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty; // Accepted, Rejected, Completed
}

public class CommissionResponseDto
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
