using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Members;

public interface IMemberRepository : IRepository<Member, Guid>
{
    Task<Member?> GetByCheckInCodeAsync(string checkInCode, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
}
