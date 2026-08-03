using GymManager.Domain.Common;
using GymManager.Domain.Trainers.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Trainers;

/// <summary>A staff member who leads classes. May optionally be linked to an <see cref="Identity.User"/> account
/// via <see cref="UserId"/>; the profile itself (specialization, availability) lives here regardless.</summary>
public sealed class Trainer : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly List<AvailabilitySlot> _availability = [];

    private Trainer()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Specialization = string.Empty;
    }

    private Trainer(Guid id, Guid branchId, string firstName, string lastName, string specialization, string? bio, string? phoneNumber, Email? email, Guid? userId)
        : base(id)
    {
        BranchId = branchId;
        FirstName = firstName;
        LastName = lastName;
        Specialization = specialization;
        Bio = bio;
        PhoneNumber = phoneNumber;
        Email = email;
        UserId = userId;
        IsActive = true;
        HireDateUtc = DateTimeOffset.UtcNow;
    }

    public Guid BranchId { get; private set; }

    public Guid? UserId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Specialization { get; private set; }

    public string? Bio { get; private set; }

    public string? PhoneNumber { get; private set; }

    public Email? Email { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset HireDateUtc { get; private set; }

    public IReadOnlyCollection<AvailabilitySlot> Availability => _availability.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Trainer Create(
        Guid branchId, string firstName, string lastName, string specialization, string? bio, string? phoneNumber, Email? email, Guid? userId) =>
        new(Guid.NewGuid(), branchId, firstName.Trim(), lastName.Trim(), specialization.Trim(), bio?.Trim(), phoneNumber?.Trim(), email, userId);

    public void UpdateProfile(string firstName, string lastName, string specialization, string? bio, string? phoneNumber, Email? email)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Specialization = specialization.Trim();
        Bio = bio?.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Email = email;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public Result AddAvailabilitySlot(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        var overlaps = _availability.Any(s =>
            s.DayOfWeek == dayOfWeek && startTime < s.EndTime && s.StartTime < endTime);

        if (overlaps)
            return Result.Failure(TrainerErrors.SlotOverlaps);

        _availability.Add(new AvailabilitySlot(dayOfWeek, startTime, endTime));
        return Result.Success();
    }

    public Result RemoveAvailabilitySlot(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        var slot = _availability.FirstOrDefault(s => s.DayOfWeek == dayOfWeek && s.StartTime == startTime && s.EndTime == endTime);
        if (slot is null)
            return Result.Failure(TrainerErrors.SlotNotFound);

        _availability.Remove(slot);
        return Result.Success();
    }

    public bool IsAvailableAt(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime) =>
        _availability.Any(s => s.DayOfWeek == dayOfWeek && startTime >= s.StartTime && endTime <= s.EndTime);

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }

    public void Delete(DateTimeOffset onUtc, string? by)
    {
        IsDeleted = true;
        DeletedOnUtc = onUtc;
        DeletedBy = by;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
    }
}
