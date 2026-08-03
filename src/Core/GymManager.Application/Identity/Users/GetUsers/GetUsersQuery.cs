using GymManager.Application.Identity.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Identity.Users.GetUsers;

public sealed record GetUsersQuery(PaginationParameters Pagination) : IQuery<PagedList<UserResponse>>;
