using IncidentReport.Application.DTOs;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Queries;

public record GetIncidentsQuery(
    string? TailNumber,
    FaultType? FaultType,
    Severity? Severity,
    IncidentStatus? Status,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? TechnicianId,
    string? SupervisorId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<IncidentSummaryDto>>;

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
