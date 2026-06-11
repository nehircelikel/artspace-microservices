using Microsoft.EntityFrameworkCore;
using RequestService.Core.Entities;
using RequestService.Core.Interfaces;
using RequestService.Infrastructure.Data;

namespace RequestService.Infrastructure.Services;

public class ArtworkRequestRepository : IArtworkRequestRepository
{
    private readonly RequestServiceContext _context;

    public ArtworkRequestRepository(RequestServiceContext context)
    {
        _context = context;
    }

    public async Task<ArtworkRequest?> GetByIdAsync(Guid id)
    {
        return await _context.ArtworkRequests
            .Include(r => r.Logs.OrderBy(l => l.CreatedAt))
            .Include(r => r.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<ArtworkRequest>> GetReceivedAsync(Guid artistId)
    {
        return await _context.ArtworkRequests
            .Where(r => r.ArtistId == artistId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ArtworkRequest>> GetSentAsync(Guid requesterId)
    {
        return await _context.ArtworkRequests
            .Where(r => r.RequesterId == requesterId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ArtworkRequest> CreateAsync(ArtworkRequest request)
    {
        _context.ArtworkRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<ArtworkRequest> UpdateAsync(ArtworkRequest request)
    {
        _context.ArtworkRequests.Update(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task AddLogAsync(RequestLog log)
    {
        _context.RequestLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<RequestMessage> AddMessageAsync(RequestMessage message)
    {
        _context.RequestMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }
}
