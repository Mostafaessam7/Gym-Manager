using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.DeleteMember;

public sealed record DeleteMemberCommand(Guid MemberId) : ICommand;
