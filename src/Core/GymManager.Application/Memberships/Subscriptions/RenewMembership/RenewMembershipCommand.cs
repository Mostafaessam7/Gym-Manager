using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Subscriptions.RenewMembership;

public sealed record RenewMembershipCommand(Guid MembershipId, int AdditionalDays, decimal AmountPaid, string Currency)
    : ICommand<Result<MembershipResponse>>;
