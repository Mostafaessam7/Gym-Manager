using GymManager.Application.Abstractions;
using GymManager.Application.Members.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.CreateMember;

public sealed class CreateMemberCommandHandler(
    IMemberRepository memberRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CreateMemberCommand, Result<MemberResponse>>
{
    public async Task<Result<MemberResponse>> Handle(CreateMemberCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<MemberResponse>(accessResult.Error);

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailResult = Email.Create(command.Email);
            if (emailResult.IsFailure)
                return Result.Failure<MemberResponse>(emailResult.Error);

            if (await memberRepository.EmailExistsAsync(emailResult.Value.Value, cancellationToken))
                return Result.Failure<MemberResponse>(MemberErrors.EmailAlreadyInUse(emailResult.Value.Value));

            email = emailResult.Value;
        }

        Address? address = string.IsNullOrWhiteSpace(command.Country)
            ? null
            : Address.Create(command.Country, command.Street, command.City, command.State, command.PostalCode);

        var nextNumber = await memberRepository.GetTotalCountAsync(cancellationToken) + 1;
        var memberCode = $"MEM-{nextNumber:D6}";

        var member = Member.Register(
            memberCode, command.BranchId, command.FirstName, command.LastName, command.PhoneNumber,
            email, command.DateOfBirth, command.Gender, address);

        member.UpdateEmergencyContact(command.EmergencyContactName, command.EmergencyContactPhone);

        memberRepository.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(member.ToResponse());
    }
}
