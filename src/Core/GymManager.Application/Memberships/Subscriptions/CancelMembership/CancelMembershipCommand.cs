using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Subscriptions.CancelMembership;

public sealed record CancelMembershipCommand(Guid MembershipId) : ICommand;
