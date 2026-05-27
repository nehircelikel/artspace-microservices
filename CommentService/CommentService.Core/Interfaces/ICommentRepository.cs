using CommentService.Core.Entities;

namespace CommentService.Core.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Comment>> GetByArtworkIdAsync(Guid artworkId);
    Task<IEnumerable<Comment>> GetByUserIdAsync(Guid userId);
    Task<Comment> CreateAsync(Comment comment);
    Task DeleteAsync(Guid id);
    Task<double> GetAverageRatingAsync(Guid artworkId);
}