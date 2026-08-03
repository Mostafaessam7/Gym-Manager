using GymManager.Domain.Classes;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class ClassSessionRepository(GymManagerDbContext dbContext) : IClassSessionRepository
{
    // Bookings is an owned collection mapped to its own table; without this Include, EF Core never loads
    // it, so Book()/CancelBooking() would mutate a list the change tracker isn't watching.
    public Task<ClassSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ClassSessions.Include(s => s.Bookings).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> TrainerHasOverlappingSessionAsync(
        Guid trainerId, DateTimeOffset startUtc, DateTimeOffset endUtc, Guid? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ClassSessions.Where(s =>
            s.TrainerId == trainerId &&
            s.Status != ClassSessionStatus.Cancelled &&
            startUtc < s.EndUtc && s.StartUtc < endUtc);

        if (excludeSessionId.HasValue)
            query = query.Where(s => s.Id != excludeSessionId);

        return query.AnyAsync(cancellationToken);
    }

    public void Add(ClassSession aggregate) => dbContext.ClassSessions.Add(aggregate);

    public void Update(ClassSession aggregate) => dbContext.ClassSessions.Update(aggregate);

    public void Remove(ClassSession aggregate) => dbContext.ClassSessions.Remove(aggregate);
}
