using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestService.API.DTOs;
using RequestService.Core.Entities;
using RequestService.Core.Interfaces;
using RequestService.Infrastructure.Services;
using System.Security.Claims;

namespace RequestService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestController : ControllerBase
{
    private readonly IArtworkRequestRepository _repository;
    private readonly IRabbitMQPublisher _publisher;

    public RequestController(IArtworkRequestRepository repository, IRabbitMQPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    private string CurrentEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? "";

    // GET /api/Request/received — requests addressed to the signed-in artist.
    [HttpGet("received")]
    public async Task<ActionResult<IEnumerable<RequestResponseDto>>> GetReceived()
    {
        var requests = await _repository.GetReceivedAsync(CurrentUserId);
        return Ok(requests.Select(MapToDto));
    }

    // GET /api/Request/sent — requests the signed-in user has sent.
    [HttpGet("sent")]
    public async Task<ActionResult<IEnumerable<RequestResponseDto>>> GetSent()
    {
        var requests = await _repository.GetSentAsync(CurrentUserId);
        return Ok(requests.Select(MapToDto));
    }

    // GET /api/Request/{id} — only the artist or the requester may view a request.
    [HttpGet("{id}")]
    public async Task<ActionResult<RequestResponseDto>> GetById(Guid id)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return NotFound();
        if (!IsParticipant(request)) return Forbid();
        return Ok(MapToDto(request));
    }

    [HttpPost]
    public async Task<ActionResult<RequestResponseDto>> Create(CreateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");
        if (dto.ArtistId == Guid.Empty)
            return BadRequest("An artist must be specified.");
        if (dto.ArtistId == CurrentUserId)
            return BadRequest("You cannot send an artwork request to yourself.");

        var request = new ArtworkRequest
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Budget = dto.Budget,
            Deadline = dto.Deadline,
            Status = RequestStatus.Pending,
            ArtworkId = dto.ArtworkId,
            RequesterId = CurrentUserId,
            RequesterUsername = CurrentUsername,
            RequesterEmail = CurrentEmail,
            ArtistId = dto.ArtistId,
            ArtistUsername = dto.ArtistUsername,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(request);
        await Log(created.Id, $"{CurrentUsername} sent the request");

        _publisher.PublishNotification(
            created.ArtistId,
            $"{CurrentUsername} sent you an artwork request: {created.Title}");

        var full = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(full!));
    }

    // PUT /api/Request/{id} — edit the fields the caller owns. Only allowed while the
    // request is still Pending.
    [HttpPut("{id}")]
    public async Task<ActionResult<RequestResponseDto>> Update(Guid id, UpdateRequestDto dto)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return NotFound();
        if (!IsParticipant(request)) return Forbid();
        if (request.Status != RequestStatus.Pending)
            return BadRequest("Only pending requests can be edited.");

        var isArtist = request.ArtistId == CurrentUserId;
        var changes = new List<string>();

        if (isArtist)
        {
            if (dto.Description != null && dto.Description != request.Description)
            {
                request.Description = dto.Description;
                changes.Add("description");
            }
            if (dto.EstimatedTime != null && dto.EstimatedTime != request.EstimatedTime)
            {
                request.EstimatedTime = dto.EstimatedTime;
                changes.Add("estimated time");
            }
            if (dto.EstimatedCost != null && dto.EstimatedCost != request.EstimatedCost)
            {
                request.EstimatedCost = dto.EstimatedCost;
                changes.Add("estimated cost");
            }
        }
        else // requester (client)
        {
            if (dto.Budget != null && dto.Budget != request.Budget)
            {
                request.Budget = dto.Budget;
                changes.Add("budget");
            }
            if (dto.Deadline != null && dto.Deadline != request.Deadline)
            {
                request.Deadline = dto.Deadline;
                changes.Add("deadline");
            }
        }

        if (changes.Count == 0)
            return Ok(MapToDto(request));

        request.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(request);
        await Log(request.Id, $"{CurrentUsername} updated {string.Join(", ", changes)}");

        var full = await _repository.GetByIdAsync(request.Id);
        return Ok(MapToDto(full!));
    }

    // POST /api/Request/{id}/accept — artist only; requires estimated cost + time.
    [HttpPost("{id}/accept")]
    public Task<ActionResult<RequestResponseDto>> Accept(Guid id) =>
        ArtistDecision(id, RequestStatus.Accepted, requireEstimates: true,
            requesterMessage: r => $"{r.ArtistUsername} accepted your request: {r.Title}");

    // POST /api/Request/{id}/decline — artist only.
    [HttpPost("{id}/decline")]
    public Task<ActionResult<RequestResponseDto>> Decline(Guid id) =>
        ArtistDecision(id, RequestStatus.Declined, requireEstimates: false,
            requesterMessage: r => $"{r.ArtistUsername} declined your request: {r.Title}");

    // POST /api/Request/{id}/complete — artist marks an accepted request done.
    [HttpPost("{id}/complete")]
    public async Task<ActionResult<RequestResponseDto>> Complete(Guid id)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return NotFound();
        if (request.ArtistId != CurrentUserId) return Forbid();
        if (request.Status != RequestStatus.Accepted)
            return BadRequest("Only an accepted request can be completed.");

        await Transition(request, RequestStatus.Completed, $"{CurrentUsername} marked the request completed");
        _publisher.PublishNotification(
            request.RequesterId,
            $"{request.ArtistUsername} completed your request: {request.Title}");

        return Ok(MapToDto((await _repository.GetByIdAsync(id))!));
    }

    // POST /api/Request/{id}/withdraw — requester only, while still pending.
    [HttpPost("{id}/withdraw")]
    public async Task<ActionResult<RequestResponseDto>> Withdraw(Guid id)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return NotFound();
        if (request.RequesterId != CurrentUserId) return Forbid();
        if (request.Status != RequestStatus.Pending)
            return BadRequest("Only a pending request can be withdrawn.");

        await Transition(request, RequestStatus.Withdrawn, $"{CurrentUsername} withdrew the request");
        _publisher.PublishNotification(
            request.ArtistId,
            $"{request.RequesterUsername} withdrew their request: {request.Title}");

        return Ok(MapToDto((await _repository.GetByIdAsync(id))!));
    }

    // POST /api/Request/{id}/messages — either participant adds a message; the other
    // participant is notified.
    [HttpPost("{id}/messages")]
    public async Task<ActionResult<RequestMessageDto>> AddMessage(Guid id, CreateMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Message content is required.");

        var request = await _repository.GetByIdAsync(id);
        if (request == null) return NotFound();
        if (!IsParticipant(request)) return Forbid();

        var message = new RequestMessage
        {
            Id = Guid.NewGuid(),
            RequestId = request.Id,
            SenderId = CurrentUserId,
            SenderUsername = CurrentUsername,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddMessageAsync(message);

        var recipientId = request.ArtistId == CurrentUserId ? request.RequesterId : request.ArtistId;
        _publisher.PublishNotification(
            recipientId,
            $"{CurrentUsername} sent a message on the request: {request.Title}");

        return Ok(MapMessage(created));
    }

    // Shared accept/decline path: artist-only, pending-only, optional estimate gate.
    private async Task<ActionResult<RequestResponseDto>> ArtistDecision(
        Guid id, RequestStatus target, bool requireEstimates, Func<ArtworkRequest, string> requesterMessage)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return NotFound();
        if (request.ArtistId != CurrentUserId) return Forbid();
        if (request.Status != RequestStatus.Pending)
            return BadRequest("Only a pending request can be accepted or declined.");

        if (requireEstimates && (request.EstimatedCost == null || string.IsNullOrWhiteSpace(request.EstimatedTime)))
            return BadRequest("Set an estimated cost and time before accepting.");

        await Transition(request, target, $"{CurrentUsername} {target.ToString().ToLowerInvariant()} the request");
        _publisher.PublishNotification(request.RequesterId, requesterMessage(request));

        return Ok(MapToDto((await _repository.GetByIdAsync(id))!));
    }

    private async Task Transition(ArtworkRequest request, RequestStatus target, string logAction)
    {
        request.Status = target;
        request.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(request);
        await Log(request.Id, logAction);
    }

    private Task Log(Guid requestId, string action) =>
        _repository.AddLogAsync(new RequestLog
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            Action = action,
            ActorUsername = CurrentUsername,
            CreatedAt = DateTime.UtcNow
        });

    private bool IsParticipant(ArtworkRequest request) =>
        request.ArtistId == CurrentUserId || request.RequesterId == CurrentUserId;

    private static RequestResponseDto MapToDto(ArtworkRequest r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        Budget = r.Budget,
        Deadline = r.Deadline,
        EstimatedTime = r.EstimatedTime,
        EstimatedCost = r.EstimatedCost,
        Status = r.Status.ToString(),
        ArtworkId = r.ArtworkId,
        RequesterId = r.RequesterId,
        RequesterUsername = r.RequesterUsername,
        RequesterEmail = r.RequesterEmail,
        ArtistId = r.ArtistId,
        ArtistUsername = r.ArtistUsername,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        Logs = r.Logs.OrderBy(l => l.CreatedAt).Select(l => new RequestLogDto
        {
            Id = l.Id,
            Action = l.Action,
            ActorUsername = l.ActorUsername,
            CreatedAt = l.CreatedAt
        }).ToList(),
        Messages = r.Messages.OrderBy(m => m.CreatedAt).Select(MapMessage).ToList()
    };

    private static RequestMessageDto MapMessage(RequestMessage m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderUsername = m.SenderUsername,
        Content = m.Content,
        CreatedAt = m.CreatedAt
    };
}
