using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.UpdateMember;

public sealed class UpdateMemberCommandHandler(
    IMemberRepository memberRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateMemberCommand>
{
    public async Task<Result> Handle(UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailResult = Email.Create(command.Email);
            if (emailResult.IsFailure)
                return Result.Failure(emailResult.Error);

            if (emailResult.Value.Value != member.Email?.Value &&
                await memberRepository.EmailExistsAsync(emailResult.Value.Value, cancellationToken))
                return Result.Failure(MemberErrors.EmailAlreadyInUse(emailResult.Value.Value));

            email = emailResult.Value;
        }

        Address? address = string.IsNullOrWhiteSpace(command.Country)
            ? null
            : Address.Create(command.Country, command.Street, command.City, command.State, command.PostalCode);

        member.UpdateProfile(command.FirstName, command.LastName, command.PhoneNumber, email, command.DateOfBirth, command.Gender, address);
        member.UpdateEmergencyContact(command.EmergencyContactName, command.EmergencyContactPhone);

        memberRepository.Update(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
