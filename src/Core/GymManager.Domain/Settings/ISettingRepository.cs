using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Settings;

public interface ISettingRepository : IRepository<Setting, Guid>
{
    Task<Setting?> GetByKeyAsync(string key, Guid? branchId, CancellationToken cancellationToken = default);

    Task<bool> KeyExistsAsync(string key, Guid? branchId, CancellationToken cancellationToken = default);
}
