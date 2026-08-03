using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Dashboard;

public sealed record GetDashboardSummaryQuery(Guid? BranchId) : IQuery<DashboardSummaryResponse>;
