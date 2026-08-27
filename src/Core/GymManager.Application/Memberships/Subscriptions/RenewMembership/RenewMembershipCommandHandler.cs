using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Subscriptions.RenewMembership;

public sealed class RenewMembershipCommandHandler(
    IMembershipRepository membershipRepository,
    IMemberRepository memberRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RenewMembershipCommand, Result<MembershipResponse>>
{
    public async Task<Result<MembershipResponse>> Handle(RenewMembershipCommand command, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return Result.Failure<MembershipResponse>(MembershipErrors.NotFound);

        var memberBranchId = await memberRepository.GetBranchIdForAuthorizationAsync(membership.MemberId, cancellationToken);
        if (memberBranchId is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(memberBranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<MembershipResponse>(accessResult.Error);
        }

        var amountResult = Money.Create(command.AmountPaid, command.Currency);
        if (amountResult.IsFailure)
            return Result.Failure<MembershipResponse>(amountResult.Error);

        var result = membership.Renew(command.AdditionalDays, amountResult.Value, dateTimeProvider.TodayUtc);
        if (result.IsFailure)
            return Result.Failure<MembershipResponse>(result.Error);

        membershipRepository.Update(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(membership.ToResponse());
    }
}
