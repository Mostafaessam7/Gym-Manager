using GymManager.Domain.Abstractions;
using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.BodyMeasurements.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.BodyMeasurements.DeleteBodyMeasurement;

public sealed class DeleteBodyMeasurementCommandHandler(IBodyMeasurementRepository bodyMeasurementRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteBodyMeasurementCommand>
{
    public async Task<Result> Handle(DeleteBodyMeasurementCommand command, CancellationToken cancellationToken)
    {
        var measurement = await bodyMeasurementRepository.GetByIdAsync(command.MeasurementId, cancellationToken);
        if (measurement is null)
            return Result.Failure(BodyMeasurementErrors.NotFound);

        bodyMeasurementRepository.Remove(measurement);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
