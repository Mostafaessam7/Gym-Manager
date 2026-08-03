using GymManager.Application.Members.Contracts;
using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.UploadMemberDocument;

/// <summary>Records a document already saved via <c>POST /files</c> (the generic
/// <see cref="GymManager.Application.Abstractions.IFileStorageService"/>-backed upload endpoint) against a
/// member's profile — the same "upload first, then attach the returned URL" flow already used for profile
/// images, rather than duplicating file-handling inside this command.</summary>
public sealed record UploadMemberDocumentCommand(Guid MemberId, string FileName, string FileUrl, MemberDocumentType DocumentType)
    : ICommand<Result<MemberDocumentResponse>>;
