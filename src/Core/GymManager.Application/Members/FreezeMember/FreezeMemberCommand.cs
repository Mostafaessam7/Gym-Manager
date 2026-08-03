using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.FreezeMember;

public sealed record FreezeMemberCommand(Guid MemberId) : ICommand;
