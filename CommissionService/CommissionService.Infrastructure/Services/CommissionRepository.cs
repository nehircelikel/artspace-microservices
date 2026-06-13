using CommissionService.Core.Entities;
using CommissionService.Core.Interfaces;
using CommissionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommissionService.Infrastructure.Services;

public class CommissionRepository : ICommissionRepository
{
    private readonly CommissionServiceContext _context;

    public CommissionRepository(CommissionServiceContext context)
    {
        _context = context;
    }

    public async Task<Commission?> GetByIdAsync(Guid id)
    {
        return await _context.Commissions.FindAsync(id);
    }

    public async Task<IEnumerable<Commission>> GetReceivedAsync(Guid artistId)
    {
        return await _context.Commissions
            .Where(c => c.ArtistId == artistId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Commission>> GetSentAsync(Guid requesterId)
    {
        return await _context.Commissions
            .Where(c => c.RequesterId == requesterId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Commission> CreateAsync(Commission commission)
    {
        _context.Commissions.Add(commission);
        await _context.SaveChangesAsync();
        return commission;
    }

    public async Task<Commission> UpdateAsync(Commission commission)
    {
        _context.Commissions.Update(commission);
        await _context.SaveChangesAsync();
        return commission;
    }
}
