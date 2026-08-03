using GymManager.Application.Abstractions;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Attendance.GetMemberCheckInCode;

public sealed class GetMemberCheckInCodeQueryHandler(IApplicationReadDb readDb, IQrCodeGenerator qrCodeGenerator)
    : IQueryHandler<GetMemberCheckInCodeQuery, Result<MemberCheckInCodeResponse>>
{
    public async Task<Result<MemberCheckInCodeResponse>> Handle(GetMemberCheckInCodeQuery query, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == query.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<MemberCheckInCodeResponse>(MemberErrors.NotFound);

        var qrPng = qrCodeGenerator.GeneratePng(member.CheckInCode);

        return Result.Success(new MemberCheckInCodeResponse(member.CheckInCode, qrPng));
    }
}
