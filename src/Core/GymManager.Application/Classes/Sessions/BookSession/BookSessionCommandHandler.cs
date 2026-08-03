using GymManager.Application.Abstractions;
using GymManager.Application.Classes.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Classes;
using GymManager.Domain.Classes.Errors;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Memberships;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.BookSession;

public sealed class BookSessionCommandHandler(
    IClassSessionRepository sessionRepository,
    IMemberRepository memberRepository,
    IMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<BookSessionCommand, Result<ClassSessionResponse>>
{
    public async Task<Result<ClassSessionResponse>> Handle(BookSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure<ClassSessionResponse>(ClassSessionErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(session.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<ClassSessionResponse>(accessResult.Error);

        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<ClassSessionResponse>(MemberErrors.NotFound);

        if (member.Status != MemberStatus.Active)
            return Result.Failure<ClassSessionResponse>(ClassSessionErrors.MemberNotActive);

        var activeMembership = await membershipRepository.GetActiveByMemberIdAsync(member.Id, cancellationToken);
        if (activeMembership is null || !activeMembership.IsCurrentlyActive(DateOnly.FromDateTime(DateTime.UtcNow)))
            return Result.Failure<ClassSessionResponse>(ClassSessionErrors.MembershipNotActive);

        var result = session.Book(member.Id);
        if (result.IsFailure)
            return Result.Failure<ClassSessionResponse>(result.Error);

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session.ToResponse());
    }
}
