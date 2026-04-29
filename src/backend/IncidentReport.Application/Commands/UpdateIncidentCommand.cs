using IncidentReport.Application.DTOs;
using MediatR;

namespace IncidentReport.Application.Commands;

public record UpdateIncidentCommand(
    Guid IncidentId,
    string Description,
    string StepsToReproduce,
    string TechnicianId,
    string SupervisorId,
    string ActorId,
    string ActorName
) : IRequest<IncidentDto>;
