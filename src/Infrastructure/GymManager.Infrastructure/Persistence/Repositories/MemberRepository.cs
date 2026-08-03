using GymManager.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class MemberRepository(GymManagerDbContext dbContext) : IMemberRepository
{
    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Members.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Member?> GetByCheckInCodeAsync(string checkInCode, CancellationToken cancellationToken = default) =>
        dbContext.Members.FirstOrDefaultAsync(m => m.CheckInCode == checkInCode, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Members.AnyAsync(m => m.Email != null && m.Email.Value == email, cancellationToken);

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default) =>
        dbContext.Members.CountAsync(cancellationToken);

    public void Add(Member aggregate) => dbContext.Members.Add(aggregate);

    public void Update(Member aggregate) => dbContext.Members.Update(aggregate);

    public void Remove(Member aggregate) => dbContext.Members.Remove(aggregate);
}
