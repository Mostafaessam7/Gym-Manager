using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Classes;

public interface IClassSessionRepository : IRepository<ClassSession, Guid>
{
    Task<bool> TrainerHasOverlappingSessionAsync(
        Guid trainerId, DateTimeOffset startUtc, DateTimeOffset endUtc, Guid? excludeSessionId = null, CancellationToken cancellationToken = default);
}
