using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.UpdateProfileImage;

public sealed class UpdateProfileImageCommandHandler(
    IMemberRepository memberRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateProfileImageCommand>
{
    public async Task<Result> Handle(UpdateProfileImageCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        member.UpdateProfileImage(command.ImageUrl);

        memberRepository.Update(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
