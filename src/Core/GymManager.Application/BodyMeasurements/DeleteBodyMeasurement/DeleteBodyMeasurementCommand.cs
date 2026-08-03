using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.BodyMeasurements.DeleteBodyMeasurement;

public sealed record DeleteBodyMeasurementCommand(Guid MeasurementId) : ICommand;
