using GymManager.Application.Abstractions;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Attendance.GetMemberBarcode;

public sealed class GetMemberBarcodeQueryHandler(IApplicationReadDb readDb, IBarcodeGenerator barcodeGenerator, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetMemberBarcodeQuery, Result<MemberBarcodeResponse>>
{
    public async Task<Result<MemberBarcodeResponse>> Handle(GetMemberBarcodeQuery query, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == query.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<MemberBarcodeResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<MemberBarcodeResponse>(accessResult.Error);

        var barcodePng = barcodeGenerator.GeneratePng(member.CheckInCode);

        return Result.Success(new MemberBarcodeResponse(member.CheckInCode, barcodePng));
    }
}
