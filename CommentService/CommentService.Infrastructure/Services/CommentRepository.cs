using CommentService.Core.Entities;
using CommentService.Core.Interfaces;
using CommentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Infrastructure.Services;

public class CommentRepository : ICommentRepository
{
    private readonly CommentServiceContext _context;

    public CommentRepository(CommentServiceContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(Guid id)
    {
        return await _context.Comments.FindAsync(id);
    }

    public async Task<IEnumerable<Comment>> GetByArtworkIdAsync(Guid artworkId)
    {
        // Top-level reviews newest-first, each with its replies oldest-first.
        return await _context.Comments
            .Where(c => c.ArtworkId == artworkId && c.ParentId == null)
            .Include(c => c.Replies.OrderBy(r => r.CreatedAt))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Comments
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment> CreateAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task<Comment> UpdateAsync(Comment comment)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task DeleteAsync(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<double> GetAverageRatingAsync(Guid artworkId)
    {
        // Only reviews (parents with a rating) count toward the overall rating.
        var ratings = await _context.Comments
            .Where(c => c.ArtworkId == artworkId && c.ParentId == null && c.Rating != null)
            .Select(c => c.Rating!.Value)
            .ToListAsync();

        if (ratings.Count == 0) return 0;
        return ratings.Average();
    }

    public async Task<int> GetRatingCountAsync(Guid artworkId)
    {
        return await _context.Comments
            .CountAsync(c => c.ArtworkId == artworkId && c.ParentId == null && c.Rating != null);
    }

    public async Task<bool> HasUserReviewedAsync(Guid artworkId, Guid userId)
    {
        // A review is a top-level comment (ParentId == null); replies don't count.
        return await _context.Comments
            .AnyAsync(c => c.ArtworkId == artworkId && c.UserId == userId && c.ParentId == null);
    }

    public async Task<IEnumerable<(Guid ArtworkId, double AverageRating, int RatingCount)>> GetRatingsForArtworksAsync(IEnumerable<Guid> artworkIds)
    {
        var ids = artworkIds.Distinct().ToList();
        if (ids.Count == 0)
            return Enumerable.Empty<(Guid, double, int)>();

        var grouped = await _context.Comments
            .Where(c => ids.Contains(c.ArtworkId) && c.ParentId == null && c.Rating != null)
            .GroupBy(c => c.ArtworkId)
            .Select(g => new
            {
                ArtworkId = g.Key,
                AverageRating = g.Average(c => (double)c.Rating!.Value),
                RatingCount = g.Count()
            })
            .ToListAsync();

        return grouped.Select(g => (g.ArtworkId, g.AverageRating, g.RatingCount));
    }
}
