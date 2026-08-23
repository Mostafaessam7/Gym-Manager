using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.BodyMeasurements.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.BodyMeasurements.UpdateBodyMeasurement;

public sealed class UpdateBodyMeasurementCommandHandler(
    IBodyMeasurementRepository bodyMeasurementRepository, IUnitOfWork unitOfWork, IApplicationReadDb readDb,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateBodyMeasurementCommand>
{
    public async Task<Result> Handle(UpdateBodyMeasurementCommand command, CancellationToken cancellationToken)
    {
        var measurement = await bodyMeasurementRepository.GetByIdAsync(command.MeasurementId, cancellationToken);
        if (measurement is null)
            return Result.Failure(BodyMeasurementErrors.NotFound);

        // Guid.Empty is deliberate when the owning member no longer resolves (e.g. deleted since this
        // measurement was recorded — BodyMeasurement has no FK to Member, so it can outlive it): a
        // branch-scoped caller (whose claim never equals Guid.Empty) is denied, exactly as if the orphaned
        // measurement belonged to some other branch, instead of the check being skipped entirely.
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == measurement.MemberId, cancellationToken);
        var accessResult = branchAccessGuard.EnsureCanAccess(member?.BranchId ?? Guid.Empty);
        if (accessResult.IsFailure)
            return Result.Failure(accessResult.Error);

        measurement.Update(
            command.RecordedOnUtc, command.HeightCm, command.WeightKg, command.BodyFatPercentage,
            command.ChestCm, command.WaistCm, command.HipsCm, command.ArmCm, command.ThighCm, command.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
