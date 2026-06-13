namespace CommissionService.Core.Entities;

public class Commission
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public Guid ArtistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Completed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
