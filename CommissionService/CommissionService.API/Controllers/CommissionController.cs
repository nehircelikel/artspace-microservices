using CommissionService.API.DTOs;
using CommissionService.Core.Entities;
using CommissionService.Core.Interfaces;
using CommissionService.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CommissionService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommissionController : ControllerBase
{
    private readonly ICommissionRepository _commissionRepository;
    private readonly RabbitMQPublisher _publisher;

    public CommissionController(ICommissionRepository commissionRepository, RabbitMQPublisher publisher)
    {
        _commissionRepository = commissionRepository;
        _publisher = publisher;
    }

    // Create a new commission request.
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CommissionResponseDto>> Create(CreateCommissionDto dto)
    {
        var requesterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var requesterUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (dto.ArtistId == requesterId)
            return BadRequest("Kendinize komisyon talebi gönderemezsiniz.");

        var commission = new Commission
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            RequesterUsername = requesterUsername,
            ArtistId = dto.ArtistId,
            Title = dto.Title,
            Description = dto.Description,
            Budget = dto.Budget,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commissionRepository.CreateAsync(commission);

        // Notify the artist via RabbitMQ
        _publisher.Publish(new
        {
            ArtistId = dto.ArtistId,
            Username = requesterUsername,
            Content = $"sent you a commission request: {dto.Title}"
        });

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<CommissionResponseDto>> GetById(Guid id)
    {
        var commission = await _commissionRepository.GetByIdAsync(id);
        if (commission == null) return NotFound();
        return Ok(MapToDto(commission));
    }

    // Commission requests received by the current user (as an artist).
    [HttpGet("received")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CommissionResponseDto>>> GetReceived()
    {
        var artistId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var commissions = await _commissionRepository.GetReceivedAsync(artistId);
        return Ok(commissions.Select(MapToDto));
    }

    // Commission requests sent by the current user.
    [HttpGet("sent")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CommissionResponseDto>>> GetSent()
    {
        var requesterId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var commissions = await _commissionRepository.GetSentAsync(requesterId);
        return Ok(commissions.Select(MapToDto));
    }

    // Artist accepts/rejects/completes a commission. Only the target artist may change status.
    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<ActionResult<CommissionResponseDto>> UpdateStatus(Guid id, UpdateStatusDto dto)
    {
        var commission = await _commissionRepository.GetByIdAsync(id);
        if (commission == null) return NotFound();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (commission.ArtistId != userId) return Forbid();

        var allowed = new[] { "Accepted", "Rejected", "Completed" };
        if (!allowed.Contains(dto.Status))
            return BadRequest("Geçersiz durum. Accepted, Rejected veya Completed olmalıdır.");

        commission.Status = dto.Status;
        var updated = await _commissionRepository.UpdateAsync(commission);
        return Ok(MapToDto(updated));
    }

    private CommissionResponseDto MapToDto(Commission c) => new()
    {
        Id = c.Id,
        RequesterId = c.RequesterId,
        RequesterUsername = c.RequesterUsername,
        ArtistId = c.ArtistId,
        Title = c.Title,
        Description = c.Description,
        Budget = c.Budget,
        Status = c.Status,
        CreatedAt = c.CreatedAt
    };
}
