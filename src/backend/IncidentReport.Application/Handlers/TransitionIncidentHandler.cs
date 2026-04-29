using IncidentReport.Application.Commands;
using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class TransitionIncidentHandler : IRequestHandler<TransitionIncidentCommand, IncidentDto>
{
    private readonly IIncidentRepository _incidents;
    private readonly IAircraftRepository _aircraft;
    private readonly IAuditRepository _audit;

    public TransitionIncidentHandler(
        IIncidentRepository incidents,
        IAircraftRepository aircraft,
        IAuditRepository audit)
    {
        _incidents = incidents;
        _aircraft = aircraft;
        _audit = audit;
    }

    public async Task<IncidentDto> Handle(TransitionIncidentCommand request, CancellationToken ct)
    {
        var incident = await _incidents.GetByIdAsync(request.IncidentId, ct)
            ?? throw new KeyNotFoundException($"Incident '{request.IncidentId}' not found.");

        var previousStatus = incident.Status.ToString();
        incident.TransitionTo(request.NewStatus, request.ActorId, request.ActorRole);
        await _incidents.UpdateAsync(incident, ct);

        // If closing a CRITICAL incident, unground the aircraft
        if (request.NewStatus == IncidentStatus.Closed && incident.Severity == Severity.Critical)
        {
            var aircraft = await _aircraft.GetByTailNumberAsync(incident.AircraftTailNumber, ct);
            if (aircraft?.GroundedByIncidentId == incident.Id)
            {
                aircraft.Unground();
                await _aircraft.UpdateAsync(aircraft, ct);
                await _audit.AppendAsync(AuditEvent.Create(
                    "AIRCRAFT", aircraft.TailNumber, "UNGROUNDED",
                    request.ActorId, request.ActorName,
                    $"{{\"incidentId\":\"{incident.Id}\",\"status\":\"AVAILABLE\"}}"), ct);
            }
        }

        await _audit.AppendAsync(AuditEvent.Create(
            "INCIDENT", incident.Id.ToString(), "STATUS_CHANGED",
            request.ActorId, request.ActorName,
            $"{{\"newStatus\":\"{request.NewStatus}\"}}",
            $"{{\"previousStatus\":\"{previousStatus}\"}}"), ct);

        return MapToDto(incident);
    }

    private static IncidentDto MapToDto(Incident incident) =>
        new(incident.Id, incident.IncidentNumber, incident.AircraftTailNumber,
            incident.FaultType.ToString(), incident.Severity.ToString(), incident.Status.ToString(),
            incident.Description, incident.StepsToReproduce, incident.TechnicianId, incident.SupervisorId,
            incident.CreatedBy, incident.CreatedAt, incident.UpdatedAt, incident.ResolvedAt, incident.ClosedAt,
            new List<AttachmentDto>());
}
