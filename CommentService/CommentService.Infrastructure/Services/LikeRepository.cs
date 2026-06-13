using CommentService.Core.Entities;
using CommentService.Core.Interfaces;
using CommentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Infrastructure.Services;

public class LikeRepository : ILikeRepository
{
    private readonly CommentServiceContext _context;

    public LikeRepository(CommentServiceContext context)
    {
        _context = context;
    }

    public async Task<Like?> GetByArtworkAndUserAsync(Guid artworkId, Guid userId)
    {
        return await _context.Likes
            .FirstOrDefaultAsync(l => l.ArtworkId == artworkId && l.UserId == userId);
    }

    public async Task<IEnumerable<Like>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Likes
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByArtworkIdAsync(Guid artworkId)
    {
        return await _context.Likes
            .CountAsync(l => l.ArtworkId == artworkId);
    }

    public async Task<Like> CreateAsync(Like like)
    {
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
        return like;
    }

    public async Task DeleteAsync(Guid id)
    {
        var like = await _context.Likes.FindAsync(id);
        if (like != null)
        {
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
        }
    }
}
