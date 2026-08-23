using GymManager.Application.Abstractions;
using GymManager.Application.Members.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.UploadMemberDocument;

public sealed class UploadMemberDocumentCommandHandler(
    IMemberRepository memberRepository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UploadMemberDocumentCommand, Result<MemberDocumentResponse>>
{
    public async Task<Result<MemberDocumentResponse>> Handle(UploadMemberDocumentCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<MemberDocumentResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<MemberDocumentResponse>(accessResult.Error);

        var document = member.AddDocument(command.FileName, command.FileUrl, command.DocumentType, currentUserService.Email);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new MemberDocumentResponse(
            document.Id, document.FileName, document.FileUrl, document.DocumentType.ToString(), document.UploadedOnUtc));
    }
}
