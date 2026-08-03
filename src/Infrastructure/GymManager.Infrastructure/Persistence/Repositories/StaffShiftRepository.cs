using GymManager.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class StaffShiftRepository(GymManagerDbContext dbContext) : IStaffShiftRepository
{
    public Task<StaffShift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.StaffShifts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(StaffShift aggregate) => dbContext.StaffShifts.Add(aggregate);

    public void Update(StaffShift aggregate) => dbContext.StaffShifts.Update(aggregate);

    public void Remove(StaffShift aggregate) => dbContext.StaffShifts.Remove(aggregate);
}
