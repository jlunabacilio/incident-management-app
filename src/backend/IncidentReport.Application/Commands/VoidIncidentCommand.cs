using IncidentReport.Application.DTOs;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Commands;

public record VoidIncidentCommand(
    Guid IncidentId,
    string Justification,
    string ActorId,
    string ActorName,
    UserRole ActorRole
) : IRequest<IncidentDto>;
