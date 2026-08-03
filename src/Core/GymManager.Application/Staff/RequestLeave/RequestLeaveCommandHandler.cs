using GymManager.Application.Abstractions;
using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity.Errors;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Staff.RequestLeave;

public sealed class RequestLeaveCommandHandler(
    IApplicationReadDb readDb, ILeaveRequestRepository leaveRequestRepository,
    IBranchAccessGuard branchAccessGuard, IUnitOfWork unitOfWork)
    : ICommandHandler<RequestLeaveCommand, Result<LeaveRequestResponse>>
{
    public async Task<Result<LeaveRequestResponse>> Handle(RequestLeaveCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await readDb.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
        if (staffUser is null)
            return Result.Failure<LeaveRequestResponse>(UserErrors.NotFound);

        if (staffUser.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(staffUser.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<LeaveRequestResponse>(accessResult.Error);
        }

        var requestResult = LeaveRequest.Request(command.UserId, command.Type, command.StartDate, command.EndDate, command.Reason);
        if (requestResult.IsFailure)
            return Result.Failure<LeaveRequestResponse>(requestResult.Error);

        leaveRequestRepository.Add(requestResult.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(requestResult.Value.ToResponse());
    }
}
