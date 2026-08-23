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
    IApplicationReadDb readDb, IBodyMeasurementRepository bodyMeasurementRepository, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RecordBodyMeasurementCommand, Result<BodyMeasurementResponse>>
{
    public async Task<Result<BodyMeasurementResponse>> Handle(RecordBodyMeasurementCommand command, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<BodyMeasurementResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<BodyMeasurementResponse>(accessResult.Error);

        var measurement = BodyMeasurement.Record(
            command.MemberId, command.RecordedOnUtc, command.HeightCm, command.WeightKg, command.BodyFatPercentage,
            command.ChestCm, command.WaistCm, command.HipsCm, command.ArmCm, command.ThighCm, command.Notes);

        bodyMeasurementRepository.Add(measurement);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(measurement.ToResponse());
    }
}
