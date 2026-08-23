using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.UpdateMedicalInfo;

public sealed class UpdateMedicalInfoCommandHandler(
    IMemberRepository memberRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateMedicalInfoCommand>
{
    public async Task<Result> Handle(UpdateMedicalInfoCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var isEmpty =
            string.IsNullOrWhiteSpace(command.BloodType) &&
            string.IsNullOrWhiteSpace(command.Conditions) &&
            string.IsNullOrWhiteSpace(command.Allergies) &&
            string.IsNullOrWhiteSpace(command.Medications) &&
            string.IsNullOrWhiteSpace(command.Notes);

        member.UpdateMedicalInfo(isEmpty
            ? null
            : MedicalInfo.Create(command.BloodType, command.Conditions, command.Allergies, command.Medications, command.Notes));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
