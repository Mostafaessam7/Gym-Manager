using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Memberships;
using GymManager.Domain.Memberships.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Subscriptions.PurchaseMembership;

public sealed class PurchaseMembershipCommandHandler(
    IMemberRepository memberRepository,
    IMembershipPlanRepository planRepository,
    IMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<PurchaseMembershipCommand, Result<MembershipResponse>>
{
    public async Task<Result<MembershipResponse>> Handle(PurchaseMembershipCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<MembershipResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<MembershipResponse>(accessResult.Error);

        var plan = await planRepository.GetByIdAsync(command.MembershipPlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<MembershipResponse>(MembershipPlanErrors.NotFound);

        if (!plan.IsActive)
            return Result.Failure<MembershipResponse>(MembershipPlanErrors.Inactive);

        var membership = Membership.Purchase(member.Id, plan.Id, plan.Name, command.StartDate, plan.DurationInDays, plan.Price);

        membershipRepository.Add(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(membership.ToResponse());
    }
}
