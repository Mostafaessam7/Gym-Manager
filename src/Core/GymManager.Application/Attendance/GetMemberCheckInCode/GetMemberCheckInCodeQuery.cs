using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Attendance.GetMemberCheckInCode;

public sealed record GetMemberCheckInCodeQuery(Guid MemberId) : IQuery<Result<MemberCheckInCodeResponse>>;

public sealed record MemberCheckInCodeResponse(string CheckInCode, byte[] QrCodePng);
