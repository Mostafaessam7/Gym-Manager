using GymManager.Domain.Lockers;

namespace GymManager.Application.Lockers.Contracts;

public sealed record LockerResponse(Guid Id, Guid BranchId, string Number, string Status, Guid? AssignedMemberId, DateTimeOffset? AssignedOnUtc);

public static class LockerMappingExtensions
{
    public static LockerResponse ToResponse(this Locker locker) => new(
        locker.Id, locker.BranchId, locker.Number, locker.Status.ToString(), locker.AssignedMemberId, locker.AssignedOnUtc);
}
