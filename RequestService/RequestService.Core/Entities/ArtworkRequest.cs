namespace RequestService.Core.Entities;

public class ArtworkRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    // Editable by the artist after the request lands.
    public string Description { get; set; } = string.Empty;

    // Optional client-supplied figures. Budget/Deadline are editable by the client.
    public decimal? Budget { get; set; }
    public DateTime? Deadline { get; set; }

    // Optional artist-supplied figures; an accept requires both to be set.
    public string? EstimatedTime { get; set; }
    public decimal? EstimatedCost { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    // The artwork that inspired the commission (optional context).
    public Guid? ArtworkId { get; set; }

    // Requester (client) — denormalized from the JWT at creation time.
    public Guid RequesterId { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;

    // Artist — denormalized from the artwork the form was opened on.
    public Guid ArtistId { get; set; }
    public string ArtistUsername { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Set whenever a field is edited or the status changes; NULL means never touched.
    public DateTime? UpdatedAt { get; set; }

    public ICollection<RequestLog> Logs { get; set; } = new List<RequestLog>();
    public ICollection<RequestMessage> Messages { get; set; } = new List<RequestMessage>();
}
