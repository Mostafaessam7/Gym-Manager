using GymManager.Domain.Branches.Errors;
using GymManager.Domain.Common;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Branches;

/// <summary>A physical gym location. Every branch-scoped entity (members, staff, inventory) belongs to exactly one.</summary>
public sealed class Branch : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private Branch()
    {
        Name = string.Empty;
        Address = null!;
    }

    private Branch(Guid id, string name, Address address, string? phoneNumber, string? email) : base(id)
    {
        Name = name;
        Address = address;
        PhoneNumber = phoneNumber;
        Email = email;
        IsActive = true;
    }

    public string Name { get; private set; }

    public Address Address { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? Email { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Branch Create(string name, Address address, string? phoneNumber, string? email) =>
        new(Guid.NewGuid(), name.Trim(), address, phoneNumber?.Trim(), email?.Trim());

    public void UpdateDetails(string name, Address address, string? phoneNumber, string? email)
    {
        Name = name.Trim();
        Address = address;
        PhoneNumber = phoneNumber?.Trim();
        Email = email?.Trim();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(BranchErrors.AlreadyInactive);

        IsActive = false;
        return Result.Success();
    }

    public void Activate() => IsActive = true;

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
