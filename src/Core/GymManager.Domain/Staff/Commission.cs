using GymManager.Domain.Common;
using GymManager.Domain.Staff.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Staff;

/// <summary>An amount owed to a staff member for a commission-eligible activity (a personal training session,
/// a class taught, a product sale) — tracked separately from <c>Payment</c>/<c>Payroll</c> since it's what the
/// gym owes staff, not what a customer paid the gym.</summary>
public sealed class Commission : AggregateRoot<Guid>, IAuditableEntity
{
    private Commission()
    {
        Amount = null!;
    }

    private Commission(Guid id, Guid userId, Money amount, CommissionSourceType sourceType, Guid? sourceReferenceId, DateTimeOffset earnedOnUtc, string? notes)
        : base(id)
    {
        UserId = userId;
        Amount = amount;
        SourceType = sourceType;
        SourceReferenceId = sourceReferenceId;
        EarnedOnUtc = earnedOnUtc;
        Notes = notes;
        Status = CommissionStatus.Pending;
    }

    public Guid UserId { get; private set; }

    public Money Amount { get; private set; }

    public CommissionSourceType SourceType { get; private set; }

    public Guid? SourceReferenceId { get; private set; }

    public DateTimeOffset EarnedOnUtc { get; private set; }

    public string? Notes { get; private set; }

    public CommissionStatus Status { get; private set; }

    public DateTimeOffset? PaidOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Commission Record(
        Guid userId, Money amount, CommissionSourceType sourceType, Guid? sourceReferenceId, DateTimeOffset earnedOnUtc, string? notes) =>
        new(Guid.NewGuid(), userId, amount, sourceType, sourceReferenceId, earnedOnUtc, notes?.Trim());

    public Result MarkPaid(DateTimeOffset paidOnUtc)
    {
        if (Status == CommissionStatus.Paid)
            return Result.Failure(StaffErrors.CommissionAlreadyPaid);

        Status = CommissionStatus.Paid;
        PaidOnUtc = paidOnUtc;
        return Result.Success();
    }

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
}
