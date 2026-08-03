using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Attendance.GetMemberBarcode;

public sealed record GetMemberBarcodeQuery(Guid MemberId) : IQuery<Result<MemberBarcodeResponse>>;

public sealed record MemberBarcodeResponse(string CheckInCode, byte[] BarcodePng);
