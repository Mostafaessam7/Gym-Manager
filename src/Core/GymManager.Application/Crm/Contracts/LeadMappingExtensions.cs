using GymManager.Domain.Crm;

namespace GymManager.Application.Crm.Contracts;

public static class LeadMappingExtensions
{
    public static LeadResponse ToResponse(this Lead lead) => new(
        lead.Id,
        lead.Name,
        lead.Email,
        lead.Phone,
        lead.Source.ToString(),
        lead.Stage.ToString(),
        lead.BranchId,
        lead.AssignedToUserId,
        lead.Notes,
        lead.ConvertedMemberId,
        lead.LostReason,
        [.. lead.FollowUps
            .OrderBy(f => f.ScheduledOnUtc)
            .Select(f => new LeadFollowUpResponse(f.Id, f.Type.ToString(), f.ScheduledOnUtc, f.CompletedOnUtc, f.Notes, f.IsCompleted))],
        lead.CreatedOnUtc);
}
