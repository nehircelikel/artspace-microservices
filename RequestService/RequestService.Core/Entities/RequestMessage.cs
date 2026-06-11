namespace RequestService.Core.Entities;

// A message in the per-request conversation between the artist and the requester.
// This is a separate channel from the artwork comment system and never touches it.
public class RequestMessage
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public ArtworkRequest? Request { get; set; }

    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
