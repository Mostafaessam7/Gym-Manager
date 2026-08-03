using GymManager.Application.BodyMeasurements.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.BodyMeasurements.GetBodyMeasurements;

public sealed record GetBodyMeasurementsQuery(Guid MemberId, PaginationParameters Pagination) : IQuery<PagedList<BodyMeasurementResponse>>;
