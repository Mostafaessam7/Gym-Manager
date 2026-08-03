using GymManager.Domain.Common;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Expenses;

/// <summary>A recorded operating cost incurred by a branch (rent, salaries, equipment, etc.).</summary>
public sealed class Expense : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private Expense()
    {
        Description = string.Empty;
        PaidTo = string.Empty;
        Amount = null!;
    }

    private Expense(Guid id, Guid branchId, ExpenseCategory category, string description, Money amount, DateOnly expenseDate, string paidTo, Guid recordedByUserId, string? receiptUrl)
        : base(id)
    {
        BranchId = branchId;
        Category = category;
        Description = description;
        Amount = amount;
        ExpenseDate = expenseDate;
        PaidTo = paidTo;
        RecordedByUserId = recordedByUserId;
        ReceiptUrl = receiptUrl;
    }

    public Guid BranchId { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public string Description { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    public string PaidTo { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public string? ReceiptUrl { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Expense Record(
        Guid branchId, ExpenseCategory category, string description, Money amount, DateOnly expenseDate, string paidTo, Guid recordedByUserId, string? receiptUrl) =>
        new(Guid.NewGuid(), branchId, category, description.Trim(), amount, expenseDate, paidTo.Trim(), recordedByUserId, receiptUrl);

    public void Update(ExpenseCategory category, string description, Money amount, DateOnly expenseDate, string paidTo, string? receiptUrl)
    {
        Category = category;
        Description = description.Trim();
        Amount = amount;
        ExpenseDate = expenseDate;
        PaidTo = paidTo.Trim();
        ReceiptUrl = receiptUrl;
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
