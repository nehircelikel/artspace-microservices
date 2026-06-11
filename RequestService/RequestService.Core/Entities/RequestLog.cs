namespace RequestService.Core.Entities;

// An immutable audit line on a request, e.g. "janedoe updated budget".
public class RequestLog
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public ArtworkRequest? Request { get; set; }

    public string Action { get; set; } = string.Empty;
    public string ActorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
