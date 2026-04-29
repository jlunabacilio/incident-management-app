using IncidentReport.Application.DTOs;
using MediatR;

namespace IncidentReport.Application.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
