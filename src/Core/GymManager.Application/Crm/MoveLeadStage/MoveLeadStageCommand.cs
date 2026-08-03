using GymManager.Domain.Crm;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Crm.MoveLeadStage;

public sealed record MoveLeadStageCommand(Guid LeadId, LeadStage Stage) : ICommand;
