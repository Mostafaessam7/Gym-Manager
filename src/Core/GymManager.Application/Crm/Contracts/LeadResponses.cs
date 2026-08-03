namespace GymManager.Application.Crm.Contracts;

public sealed record LeadFollowUpResponse(
    Guid Id, string Type, DateTimeOffset ScheduledOnUtc, DateTimeOffset? CompletedOnUtc, string? Notes, bool IsCompleted);

public sealed record LeadResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Source,
    string Stage,
    Guid? BranchId,
    Guid? AssignedToUserId,
    string? Notes,
    Guid? ConvertedMemberId,
    string? LostReason,
    IReadOnlyCollection<LeadFollowUpResponse> FollowUps,
    DateTimeOffset CreatedOnUtc);
