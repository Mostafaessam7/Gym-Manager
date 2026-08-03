using GymManager.SharedKernel.Results;

namespace GymManager.Domain.BodyMeasurements.Errors;

public static class BodyMeasurementErrors
{
    public static readonly Error NotFound = Error.NotFound("BodyMeasurement.NotFound", "The measurement record was not found.");
}
