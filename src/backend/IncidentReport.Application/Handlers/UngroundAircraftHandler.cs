using IncidentReport.Application.Commands;
using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class UngroundAircraftHandler : IRequestHandler<UngroundAircraftCommand, bool>
{
    private readonly IAircraftRepository _aircraft;
    private readonly IAuditRepository _audit;

    public UngroundAircraftHandler(IAircraftRepository aircraft, IAuditRepository audit)
    {
        _aircraft = aircraft;
        _audit = audit;
    }

    public async Task<bool> Handle(UngroundAircraftCommand request, CancellationToken ct)
    {
        var aircraft = await _aircraft.GetByTailNumberAsync(request.TailNumber, ct)
            ?? throw new KeyNotFoundException($"Aircraft '{request.TailNumber}' not found.");

        aircraft.Unground();
        await _aircraft.UpdateAsync(aircraft, ct);

        await _audit.AppendAsync(AuditEvent.Create(
            "AIRCRAFT", aircraft.TailNumber, "UNGROUNDED",
            request.ActorId, request.ActorName,
            $"{{\"status\":\"AVAILABLE\"}}"), ct);

        return true;
    }
}
