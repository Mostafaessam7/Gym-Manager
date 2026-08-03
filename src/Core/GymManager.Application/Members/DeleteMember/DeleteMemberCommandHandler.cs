using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.DeleteMember;

public sealed class DeleteMemberCommandHandler(
    IMemberRepository memberRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeleteMemberCommand>
{
    public async Task<Result> Handle(DeleteMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        memberRepository.Remove(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
