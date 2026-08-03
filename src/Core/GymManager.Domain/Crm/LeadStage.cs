namespace GymManager.Domain.Crm;

/// <summary>Where a lead sits in the sales pipeline. <see cref="Won"/> and <see cref="Lost"/> are terminal —
/// see <see cref="Lead.Reopen"/> to move a lead back into the active pipeline.</summary>
public enum LeadStage
{
    New = 0,
    Contacted = 1,
    Qualified = 2,
    ProposalSent = 3,
    Won = 4,
    Lost = 5,
}
