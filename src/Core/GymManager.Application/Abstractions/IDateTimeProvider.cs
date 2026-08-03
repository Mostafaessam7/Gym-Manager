namespace GymManager.Application.Abstractions;

/// <summary>Abstraction over system time so application/domain logic remains deterministic in tests.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    DateOnly TodayUtc { get; }
}
