namespace RequestService.API.DTOs;

public class CreateRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public DateTime? Deadline { get; set; }

    // The artist the commission is addressed to, denormalized from the artwork.
    public Guid ArtistId { get; set; }
    public string ArtistUsername { get; set; } = string.Empty;

    // Optional: the artwork that inspired the request.
    public Guid? ArtworkId { get; set; }
}

// Field edits. Each side may only touch the fields it owns; unset (null) fields are
// left untouched. Budget/Deadline belong to the client; Description/EstimatedTime/
// EstimatedCost belong to the artist.
public class UpdateRequestDto
{
    public string? Description { get; set; }
    public decimal? Budget { get; set; }
    public DateTime? Deadline { get; set; }
    public string? EstimatedTime { get; set; }
    public decimal? EstimatedCost { get; set; }
}

public class CreateMessageDto
{
    public string Content { get; set; } = string.Empty;
}

public class RequestLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RequestMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RequestResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Budget { get; set; }
    public DateTime? Deadline { get; set; }
    public string? EstimatedTime { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ArtworkId { get; set; }

    public Guid RequesterId { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;

    public Guid ArtistId { get; set; }
    public string ArtistUsername { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<RequestLogDto> Logs { get; set; } = new();
    public List<RequestMessageDto> Messages { get; set; } = new();
}
