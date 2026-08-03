using GymManager.Application.Abstractions;
using GymManager.Application.Classes.Contracts;
using GymManager.Domain.Classes.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Classes.Sessions.GetSessionById;

public sealed class GetSessionByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetSessionByIdQuery, Result<ClassSessionResponse>>
{
    public async Task<Result<ClassSessionResponse>> Handle(GetSessionByIdQuery query, CancellationToken cancellationToken)
    {
        var session = await readDb.ClassSessions.FirstOrDefaultAsync(s => s.Id == query.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure<ClassSessionResponse>(ClassSessionErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(session.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<ClassSessionResponse>(accessResult.Error);

        return Result.Success(session.ToResponse());
    }
}
