using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Staff;
using GymManager.Domain.Staff.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Staff.RejectLeave;

public sealed class RejectLeaveCommandHandler(
    ILeaveRequestRepository leaveRequestRepository, IUserRepository userRepository,
    ICurrentUserService currentUserService, IBranchAccessGuard branchAccessGuard, IUnitOfWork unitOfWork)
    : ICommandHandler<RejectLeaveCommand>
{
    public async Task<Result> Handle(RejectLeaveCommand command, CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(command.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return Result.Failure(StaffErrors.LeaveRequestNotFound);

        var staffUser = await userRepository.GetByIdAsync(leaveRequest.UserId, cancellationToken);
        if (staffUser?.BranchId is { } staffBranchId)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(staffBranchId);
            if (accessResult.IsFailure)
                return accessResult;
        }

        var result = leaveRequest.Reject(currentUserService.UserId ?? Guid.Empty, command.Notes, DateTimeOffset.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
