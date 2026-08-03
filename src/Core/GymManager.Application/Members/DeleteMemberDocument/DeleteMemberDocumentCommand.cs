using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.DeleteMemberDocument;

public sealed record DeleteMemberDocumentCommand(Guid MemberId, Guid DocumentId) : ICommand;
