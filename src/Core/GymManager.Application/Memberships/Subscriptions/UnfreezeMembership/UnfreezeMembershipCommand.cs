using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Subscriptions.UnfreezeMembership;

public sealed record UnfreezeMembershipCommand(Guid MembershipId) : ICommand;
