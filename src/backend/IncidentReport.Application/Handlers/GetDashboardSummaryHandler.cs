using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Application.Queries;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IIncidentRepository _incidents;
    private readonly IAircraftRepository _aircraft;

    public GetDashboardSummaryHandler(IIncidentRepository incidents, IAircraftRepository aircraft)
    {
        _incidents = incidents;
        _aircraft = aircraft;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var (openIncidents, _) = await _incidents.SearchAsync(
            new IncidentSearchFilter(null, null, null, IncidentStatus.Open, null, null, null, null, 1, 1000), ct);

        var allAircraft = await _aircraft.GetAllAsync(ct);
        var grounded = allAircraft
            .Where(a => a.OperationalStatus == AircraftStatus.Grounded)
            .Select(a => a.TailNumber)
            .ToList();

        var openList = openIncidents.ToList();
        return new DashboardSummaryDto(
            openList.Count,
            openList.Count(i => i.Severity == Severity.Low),
            openList.Count(i => i.Severity == Severity.Medium),
            openList.Count(i => i.Severity == Severity.High),
            openList.Count(i => i.Severity == Severity.Critical),
            grounded);
    }
}
