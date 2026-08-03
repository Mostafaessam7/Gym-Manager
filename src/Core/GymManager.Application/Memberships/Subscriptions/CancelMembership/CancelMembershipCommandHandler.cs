using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Subscriptions.CancelMembership;

public sealed class CancelMembershipCommandHandler(
    IMembershipRepository membershipRepository, IMemberRepository memberRepository, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CancelMembershipCommand>
{
    public async Task<Result> Handle(CancelMembershipCommand command, CancellationToken cancellationToken)
    {
        var membership = await membershipRepository.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
            return Result.Failure(MembershipErrors.NotFound);

        var member = await memberRepository.GetByIdAsync(membership.MemberId, cancellationToken);
        if (member is not null)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
            if (accessResult.IsFailure)
                return accessResult;
        }

        var result = membership.Cancel();
        if (result.IsFailure)
            return result;

        membershipRepository.Update(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
