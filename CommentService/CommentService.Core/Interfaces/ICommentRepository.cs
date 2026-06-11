using CommentService.Core.Entities;

namespace CommentService.Core.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Comment>> GetByArtworkIdAsync(Guid artworkId);
    Task<IEnumerable<Comment>> GetByUserIdAsync(Guid userId);
    Task<Comment> CreateAsync(Comment comment);
    Task<Comment> UpdateAsync(Comment comment);
    Task DeleteAsync(Guid id);
    Task<double> GetAverageRatingAsync(Guid artworkId);
    Task<int> GetRatingCountAsync(Guid artworkId);
    Task<bool> HasUserReviewedAsync(Guid artworkId, Guid userId);
    Task<IEnumerable<(Guid ArtworkId, double AverageRating, int RatingCount)>> GetRatingsForArtworksAsync(IEnumerable<Guid> artworkIds);
}
