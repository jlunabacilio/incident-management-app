using IncidentReport.Application.DTOs;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Commands;

public record TransitionIncidentCommand(
    Guid IncidentId,
    IncidentStatus NewStatus,
    string ActorId,
    string ActorName,
    UserRole ActorRole
) : IRequest<IncidentDto>;
