using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.Sessions.ScheduleSession;

public sealed record ScheduleSessionCommand(Guid GymClassId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int? CapacityOverride)
    : ICommand<Result<ClassSessionResponse>>;
