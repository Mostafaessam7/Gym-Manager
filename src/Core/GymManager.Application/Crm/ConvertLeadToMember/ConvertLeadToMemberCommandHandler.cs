using GymManager.Application.Abstractions;
using GymManager.Application.Members.Contracts;
using GymManager.Application.Members.CreateMember;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Crm;
using GymManager.Domain.Crm.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.ConvertLeadToMember;

/// <summary>Delegates the actual member creation to <see cref="CreateMemberCommandHandler"/> via the
/// dispatcher rather than duplicating its branch-access check, email-uniqueness check, and member-code
/// generation here. The lead's own branch access is still checked here, since <c>command.BranchId</c> (the
/// new member's branch) does not have to match the lead's branch.</summary>
public sealed class ConvertLeadToMemberCommandHandler(
    ILeadRepository leadRepository, IDispatcher dispatcher, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<ConvertLeadToMemberCommand, Result<MemberResponse>>
{
    public async Task<Result<MemberResponse>> Handle(ConvertLeadToMemberCommand command, CancellationToken cancellationToken)
    {
        var lead = await leadRepository.GetByIdAsync(command.LeadId, cancellationToken);
        if (lead is null)
            return Result.Failure<MemberResponse>(LeadErrors.NotFound);

        if (lead.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(lead.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<MemberResponse>(accessResult.Error);
        }

        if (lead.ConvertedMemberId is not null)
            return Result.Failure<MemberResponse>(LeadErrors.AlreadyConverted);

        var createMemberResult = await dispatcher.Send(
            new CreateMemberCommand(
                command.BranchId, command.FirstName, command.LastName, command.PhoneNumber, command.Email,
                command.DateOfBirth, command.Gender, null, null, null, null, null, null, null),
            cancellationToken);

        if (createMemberResult.IsFailure)
            return createMemberResult;

        var convertResult = lead.ConvertToMember(createMemberResult.Value.Id);
        if (convertResult.IsFailure)
            return Result.Failure<MemberResponse>(convertResult.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return createMemberResult;
    }
}
