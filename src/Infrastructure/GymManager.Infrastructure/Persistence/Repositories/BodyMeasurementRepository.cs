using GymManager.Domain.BodyMeasurements;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class BodyMeasurementRepository(GymManagerDbContext dbContext) : IBodyMeasurementRepository
{
    public Task<BodyMeasurement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.BodyMeasurements.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public void Add(BodyMeasurement aggregate) => dbContext.BodyMeasurements.Add(aggregate);

    public void Update(BodyMeasurement aggregate) => dbContext.BodyMeasurements.Update(aggregate);

    public void Remove(BodyMeasurement aggregate) => dbContext.BodyMeasurements.Remove(aggregate);
}
