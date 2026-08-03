using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Subscriptions.FreezeMembership;

public sealed record FreezeMembershipCommand(Guid MembershipId) : ICommand;
