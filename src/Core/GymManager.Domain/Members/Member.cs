using GymManager.Domain.Common;
using GymManager.Domain.Members.Errors;
using GymManager.Domain.Members.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Members;

/// <summary>A gym member. Unlike <see cref="Identity.User"/>, a member does not necessarily authenticate —
/// this aggregate models the person being trained, tracked and billed.</summary>
public sealed class Member : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private readonly List<MemberDocument> _documents = [];

    private Member()
    {
        MemberCode = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PhoneNumber = string.Empty;
        CheckInCode = string.Empty;
    }

    private Member(
        Guid id,
        string memberCode,
        Guid branchId,
        string firstName,
        string lastName,
        string phoneNumber,
        Email? email,
        DateOnly? dateOfBirth,
        Gender gender,
        Address? address)
        : base(id)
    {
        MemberCode = memberCode;
        BranchId = branchId;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Email = email;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Address = address;
        Status = MemberStatus.Active;
        CheckInCode = Guid.NewGuid().ToString("N");
        JoinedOnUtc = DateTimeOffset.UtcNow;
    }

    public string MemberCode { get; private set; }

    public Guid BranchId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PhoneNumber { get; private set; }

    public Email? Email { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    public Gender Gender { get; private set; }

    public Address? Address { get; private set; }

    public string? ProfileImageUrl { get; private set; }

    public string? EmergencyContactName { get; private set; }

    public string? EmergencyContactPhone { get; private set; }

    public MedicalInfo? MedicalInfo { get; private set; }

    public IReadOnlyCollection<MemberDocument> Documents => _documents.AsReadOnly();

    public MemberStatus Status { get; private set; }

    /// <summary>The opaque token embedded in the member's QR/barcode credential for attendance check-in.</summary>
    public string CheckInCode { get; private set; }

    public DateTimeOffset JoinedOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Member Register(
        string memberCode,
        Guid branchId,
        string firstName,
        string lastName,
        string phoneNumber,
        Email? email,
        DateOnly? dateOfBirth,
        Gender gender,
        Address? address)
    {
        var member = new Member(
            Guid.NewGuid(), memberCode, branchId, firstName.Trim(), lastName.Trim(), phoneNumber.Trim(), email, dateOfBirth, gender, address);

        member.Raise(new MemberRegisteredDomainEvent(member.Id, branchId));
        return member;
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string phoneNumber,
        Email? email,
        DateOnly? dateOfBirth,
        Gender gender,
        Address? address)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber.Trim();
        Email = email;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Address = address;
    }

    public void UpdateEmergencyContact(string? name, string? phone)
    {
        EmergencyContactName = name?.Trim();
        EmergencyContactPhone = phone?.Trim();
    }

    public void UpdateProfileImage(string? url) => ProfileImageUrl = url;

    public void UpdateMedicalInfo(MedicalInfo? medicalInfo) => MedicalInfo = medicalInfo;

    public MemberDocument AddDocument(string fileName, string fileUrl, MemberDocumentType documentType, string? uploadedBy)
    {
        var document = new MemberDocument(fileName, fileUrl, documentType, uploadedBy);
        _documents.Add(document);
        return document;
    }

    public Result RemoveDocument(Guid documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
            return Result.Failure(MemberErrors.DocumentNotFound);

        _documents.Remove(document);
        return Result.Success();
    }

    public Result Freeze()
    {
        if (Status == MemberStatus.Frozen)
            return Result.Failure(MemberErrors.AlreadyFrozen);

        ChangeStatus(MemberStatus.Frozen);
        return Result.Success();
    }

    public Result Unfreeze()
    {
        if (Status != MemberStatus.Frozen)
            return Result.Failure(MemberErrors.NotFrozen);

        ChangeStatus(MemberStatus.Active);
        return Result.Success();
    }

    public void Deactivate() => ChangeStatus(MemberStatus.Inactive);

    public void Reactivate() => ChangeStatus(MemberStatus.Active);

    private void ChangeStatus(MemberStatus newStatus)
    {
        var previous = Status;
        Status = newStatus;
        Raise(new MemberStatusChangedDomainEvent(Id, previous, newStatus));
    }

    public void RegenerateCheckInCode() => CheckInCode = Guid.NewGuid().ToString("N");

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
