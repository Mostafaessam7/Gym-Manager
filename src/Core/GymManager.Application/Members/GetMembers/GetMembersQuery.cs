using GymManager.Application.Members.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Members.GetMembers;

public sealed record GetMembersQuery(PaginationParameters Pagination, Guid? BranchId, string? Status) : IQuery<PagedList<MemberResponse>>;
