using RequestService.Core.Entities;

namespace RequestService.Core.Interfaces;

public interface IArtworkRequestRepository
{
    // Detail with logs + messages eagerly loaded.
    Task<ArtworkRequest?> GetByIdAsync(Guid id);

    Task<IEnumerable<ArtworkRequest>> GetReceivedAsync(Guid artistId);
    Task<IEnumerable<ArtworkRequest>> GetSentAsync(Guid requesterId);

    Task<ArtworkRequest> CreateAsync(ArtworkRequest request);
    Task<ArtworkRequest> UpdateAsync(ArtworkRequest request);

    Task AddLogAsync(RequestLog log);
    Task<RequestMessage> AddMessageAsync(RequestMessage message);
}
