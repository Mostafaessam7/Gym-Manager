using GymManager.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class LeaveRequestRepository(GymManagerDbContext dbContext) : ILeaveRequestRepository
{
    public Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public void Add(LeaveRequest aggregate) => dbContext.LeaveRequests.Add(aggregate);

    public void Update(LeaveRequest aggregate) => dbContext.LeaveRequests.Update(aggregate);

    public void Remove(LeaveRequest aggregate) => dbContext.LeaveRequests.Remove(aggregate);
}
