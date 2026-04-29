using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Application.Queries;
using IncidentReport.Domain.Entities;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class GetIncidentsHandler : IRequestHandler<GetIncidentsQuery, PagedResult<IncidentSummaryDto>>
{
    private readonly IIncidentRepository _incidents;

    public GetIncidentsHandler(IIncidentRepository incidents) => _incidents = incidents;

    public async Task<PagedResult<IncidentSummaryDto>> Handle(GetIncidentsQuery request, CancellationToken ct)
    {
        var filter = new IncidentSearchFilter(
            request.TailNumber, request.FaultType, request.Severity, request.Status,
            request.DateFrom, request.DateTo, request.TechnicianId, request.SupervisorId,
            request.Page, request.PageSize);

        var (items, total) = await _incidents.SearchAsync(filter, ct);

        var dtos = items.Select(i => new IncidentSummaryDto(
            i.Id, i.IncidentNumber, i.AircraftTailNumber,
            i.FaultType.ToString(), i.Severity.ToString(), i.Status.ToString(),
            i.TechnicianId, i.CreatedAt)).ToList();

        return new PagedResult<IncidentSummaryDto>(dtos, total, request.Page, request.PageSize);
    }
}
