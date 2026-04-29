using MediatR;

namespace IncidentReport.Application.Commands;

public record UngroundAircraftCommand(
    string TailNumber,
    string ActorId,
    string ActorName
) : IRequest<bool>;
