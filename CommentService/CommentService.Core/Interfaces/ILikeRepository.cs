using CommentService.Core.Entities;

namespace CommentService.Core.Interfaces;

public interface ILikeRepository
{
    Task<Like?> GetByArtworkAndUserAsync(Guid artworkId, Guid userId);
    Task<IEnumerable<Like>> GetByUserIdAsync(Guid userId);
    Task<int> GetCountByArtworkIdAsync(Guid artworkId);
    Task<Like> CreateAsync(Like like);
    Task DeleteAsync(Guid id);
}
