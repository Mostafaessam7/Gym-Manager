using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Memberships;

namespace GymManager.Api.BackgroundJobs;

/// <summary>
/// Once a day, transitions any active membership whose end date has passed into <see cref="MembershipStatus.Expired"/>.
/// Membership expiry is otherwise only ever discovered lazily (on check-in or booking), so this job keeps the
/// stored status accurate for dashboards and reports even when nobody happens to query an expired member.
/// </summary>
public sealed class MembershipExpiryBackgroundService(IServiceProvider serviceProvider, ILogger<MembershipExpiryBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var membershipRepository = scope.ServiceProvider.GetRequiredService<IMembershipRepository>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var today = dateTimeProvider.TodayUtc;
        var expiredCandidates = await membershipRepository.GetActiveMembershipsExpiringBetweenAsync(
            DateOnly.MinValue, today.AddDays(-1), cancellationToken);

        foreach (var membership in expiredCandidates)
        {
            membership.MarkExpired(today);
            membershipRepository.Update(membership);
        }

        if (expiredCandidates.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Marked {Count} memberships as expired", expiredCandidates.Count);
        }
    }
}
