using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.UnfreezeMember;

public sealed record UnfreezeMemberCommand(Guid MemberId) : ICommand;
