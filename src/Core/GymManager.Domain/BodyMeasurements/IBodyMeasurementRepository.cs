using GymManager.Domain.Abstractions;

namespace GymManager.Domain.BodyMeasurements;

public interface IBodyMeasurementRepository : IRepository<BodyMeasurement, Guid>
{
}
