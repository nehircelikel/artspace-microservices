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
        return await _context.Comments
            .Where(c => c.ArtworkId == artworkId)
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
        var comments = await _context.Comments
            .Where(c => c.ArtworkId == artworkId)
            .ToListAsync();

        if (!comments.Any()) return 0;
        return comments.Average(c => c.Rating);
    }
}