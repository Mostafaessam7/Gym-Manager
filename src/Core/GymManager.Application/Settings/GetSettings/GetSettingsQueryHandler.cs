using GymManager.Application.Abstractions;
using GymManager.Application.Settings.Contracts;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Settings.GetSettings;

public sealed class GetSettingsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetSettingsQuery, IReadOnlyList<SettingResponse>>
{
    public async Task<IReadOnlyList<SettingResponse>> Handle(GetSettingsQuery query, CancellationToken cancellationToken)
    {
        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        var settings = readDb.Settings.Where(s => s.BranchId == branchId);

        var results = await settings.OrderBy(s => s.Key).ToListAsync(cancellationToken);

        return results.Select(s => s.ToResponse()).ToList();
    }
}
