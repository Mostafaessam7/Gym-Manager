using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Staff;

public interface ILeaveRequestRepository : IRepository<LeaveRequest, Guid>
{
}
