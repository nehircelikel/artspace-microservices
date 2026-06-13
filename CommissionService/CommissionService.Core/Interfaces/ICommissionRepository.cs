using CommissionService.Core.Entities;

namespace CommissionService.Core.Interfaces;

public interface ICommissionRepository
{
    Task<Commission?> GetByIdAsync(Guid id);
    Task<IEnumerable<Commission>> GetReceivedAsync(Guid artistId);
    Task<IEnumerable<Commission>> GetSentAsync(Guid requesterId);
    Task<Commission> CreateAsync(Commission commission);
    Task<Commission> UpdateAsync(Commission commission);
}
