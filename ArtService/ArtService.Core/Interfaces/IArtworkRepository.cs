using ArtService.Core.Entities;

namespace ArtService.Core.Interfaces;

public interface IArtworkRepository
{
    Task<Artwork?> GetByIdAsync(Guid id);
    Task<IEnumerable<Artwork>> GetAllAsync();
    Task<IEnumerable<Artwork>> GetByArtistIdAsync(Guid artistId);
    Task<(IEnumerable<Artwork> Items, int Total)> GetByArtistIdPagedAsync(Guid artistId, int page, int pageSize);
    Task<IEnumerable<Artwork>> SearchAsync(string? category, string? keyword);
    Task<Artwork> CreateAsync(Artwork artwork);
    Task<Artwork> UpdateAsync(Artwork artwork);
    Task DeleteAsync(Guid id);
}