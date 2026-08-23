using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Members;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.DeleteMemberDocument;

public sealed class DeleteMemberDocumentCommandHandler(
    IMemberRepository memberRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeleteMemberDocumentCommand>
{
    public async Task<Result> Handle(DeleteMemberDocumentCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(command.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var fileUrl = member.Documents.FirstOrDefault(d => d.Id == command.DocumentId)?.FileUrl;

        var result = member.RemoveDocument(command.DocumentId);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (fileUrl is not null)
            fileStorageService.Delete(fileUrl);

        return Result.Success();
    }
}
