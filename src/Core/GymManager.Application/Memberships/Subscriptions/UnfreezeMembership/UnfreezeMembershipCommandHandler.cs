using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Subscriptions.UnfreezeMembership;

public sealed class UnfreezeMembershipCommandHandler(
    IMembershipRepository membershipRepository, IMemberRepository memberRepository, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UnfreezeMembershipCommand>
{
    public async Task<Result> Handle(UnfreezeMembershipCommand command, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return Result.Failure(MembershipErrors.NotFound);

        var memberBranchId = await memberRepository.GetBranchIdForAuthorizationAsync(membership.MemberId, cancellationToken);
        if (memberBranchId is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(memberBranchId.Value);
            if (accessResult.IsFailure)
                return accessResult;
        }

        var result = membership.Unfreeze();
        if (result.IsFailure)
            return result;

        membershipRepository.Update(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
