using GymManager.Application.Abstractions;
using GymManager.Application.BodyMeasurements.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.BodyMeasurements.GetBodyMeasurements;

public sealed class GetBodyMeasurementsQueryHandler(IApplicationReadDb readDb)
    : IQueryHandler<GetBodyMeasurementsQuery, PagedList<BodyMeasurementResponse>>
{
    public async Task<PagedList<BodyMeasurementResponse>> Handle(GetBodyMeasurementsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var measurements = readDb.BodyMeasurements.Where(b => b.MemberId == query.MemberId);

        var totalCount = await measurements.CountAsync(cancellationToken);

        var page = await measurements
            .OrderByDescending(b => b.RecordedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Mapped after materialization (not inside the LINQ-to-SQL query) because BodyMeasurement.Bmi is a
        // computed property with branching logic that EF Core cannot translate to SQL.
        var items = page.Select(b => b.ToResponse()).ToList();

        return new PagedList<BodyMeasurementResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
