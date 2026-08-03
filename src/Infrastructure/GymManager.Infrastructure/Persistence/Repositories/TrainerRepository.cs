using GymManager.Domain.Trainers;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class TrainerRepository(GymManagerDbContext dbContext) : ITrainerRepository
{
    // Availability is an owned collection mapped to its own table; AddAvailabilitySlot()/RemoveAvailabilitySlot()
    // mutate it directly, so it must be loaded or the change tracker never observes the mutation.
    public Task<Trainer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Trainers.Include(t => t.Availability).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        dbContext.Trainers.AnyAsync(t => t.Email != null && t.Email.Value == email, cancellationToken);

    public void Add(Trainer aggregate) => dbContext.Trainers.Add(aggregate);

    public void Update(Trainer aggregate) => dbContext.Trainers.Update(aggregate);

    public void Remove(Trainer aggregate) => dbContext.Trainers.Remove(aggregate);
}
