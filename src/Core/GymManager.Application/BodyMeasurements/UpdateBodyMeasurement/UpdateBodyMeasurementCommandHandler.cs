using GymManager.Domain.Abstractions;
using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.BodyMeasurements.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.BodyMeasurements.UpdateBodyMeasurement;

public sealed class UpdateBodyMeasurementCommandHandler(IBodyMeasurementRepository bodyMeasurementRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateBodyMeasurementCommand>
{
    public async Task<Result> Handle(UpdateBodyMeasurementCommand command, CancellationToken cancellationToken)
    {
        var measurement = await bodyMeasurementRepository.GetByIdAsync(command.MeasurementId, cancellationToken);
        if (measurement is null)
            return Result.Failure(BodyMeasurementErrors.NotFound);

        measurement.Update(
            command.RecordedOnUtc, command.HeightCm, command.WeightKg, command.BodyFatPercentage,
            command.ChestCm, command.WaistCm, command.HipsCm, command.ArmCm, command.ThighCm, command.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
