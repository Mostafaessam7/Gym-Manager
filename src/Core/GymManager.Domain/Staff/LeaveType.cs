namespace GymManager.Domain.Staff;

public enum LeaveType
{
    Vacation = 0,
    Sick = 1,
    Unpaid = 2,
    Other = 3,
}

public enum LeaveRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
