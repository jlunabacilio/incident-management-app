using IncidentReport.Application.DTOs;
using MediatR;

namespace IncidentReport.Application.Queries;

public record GetIncidentByIdQuery(Guid Id) : IRequest<IncidentDto?>;
