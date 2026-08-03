using GymManager.Application.Members.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.GetMemberById;

public sealed record GetMemberByIdQuery(Guid MemberId) : IQuery<Result<MemberResponse>>;
