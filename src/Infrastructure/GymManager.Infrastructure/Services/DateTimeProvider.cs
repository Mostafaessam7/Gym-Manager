using GymManager.Application.Abstractions;

namespace GymManager.Infrastructure.Services;

/// <inheritdoc cref="IDateTimeProvider"/>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
}
