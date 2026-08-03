using GymManager.Application.Abstractions;
using GymManager.Application.BodyMeasurements.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.BodyMeasurements.RecordBodyMeasurement;

public sealed class RecordBodyMeasurementCommandHandler(
    IApplicationReadDb readDb, IBodyMeasurementRepository bodyMeasurementRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordBodyMeasurementCommand, Result<BodyMeasurementResponse>>
{
    public async Task<Result<BodyMeasurementResponse>> Handle(RecordBodyMeasurementCommand command, CancellationToken cancellationToken)
    {
        var memberExists = await readDb.Members.AnyAsync(m => m.Id == command.MemberId, cancellationToken);
        if (!memberExists)
            return Result.Failure<BodyMeasurementResponse>(MemberErrors.NotFound);

        var measurement = BodyMeasurement.Record(
            command.MemberId, command.RecordedOnUtc, command.HeightCm, command.WeightKg, command.BodyFatPercentage,
            command.ChestCm, command.WaistCm, command.HipsCm, command.ArmCm, command.ThighCm, command.Notes);

        bodyMeasurementRepository.Add(measurement);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(measurement.ToResponse());
    }
}
