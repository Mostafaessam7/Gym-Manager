using GymManager.Application.Members.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.GetMemberTimeline;

/// <summary>A unified, chronological activity feed for a member — check-ins, payments, and membership
/// lifecycle events — assembled from the existing read models rather than a new event-sourced entity, since
/// every underlying fact is already durably recorded on its own aggregate.</summary>
public sealed record GetMemberTimelineQuery(Guid MemberId) : IQuery<Result<IReadOnlyCollection<MemberTimelineEntryResponse>>>;
