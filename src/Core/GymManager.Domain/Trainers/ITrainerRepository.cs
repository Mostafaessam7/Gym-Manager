using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Trainers;

public interface ITrainerRepository : IRepository<Trainer, Guid>
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
