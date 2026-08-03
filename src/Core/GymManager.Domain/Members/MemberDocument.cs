using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Members;

public enum MemberDocumentType
{
    IdCard = 0,
    Waiver = 1,
    MedicalCertificate = 2,
    Contract = 3,
    Other = 4,
}

/// <summary>A file attached to a member's profile (ID scan, signed waiver, medical certificate, etc.). The
/// file itself lives in <c>IFileStorageService</c>-managed storage; this entity only tracks the
/// pointer and metadata.</summary>
public sealed class MemberDocument : Entity<Guid>
{
    private MemberDocument()
    {
        FileName = string.Empty;
        FileUrl = string.Empty;
    }

    internal MemberDocument(string fileName, string fileUrl, MemberDocumentType documentType, string? uploadedBy)
        : base(Guid.NewGuid())
    {
        FileName = fileName;
        FileUrl = fileUrl;
        DocumentType = documentType;
        UploadedOnUtc = DateTimeOffset.UtcNow;
        UploadedBy = uploadedBy;
    }

    public string FileName { get; private set; }

    public string FileUrl { get; private set; }

    public MemberDocumentType DocumentType { get; private set; }

    public DateTimeOffset UploadedOnUtc { get; private set; }

    public string? UploadedBy { get; private set; }
}
