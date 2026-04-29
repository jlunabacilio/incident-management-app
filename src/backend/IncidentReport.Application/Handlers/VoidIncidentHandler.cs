using IncidentReport.Application.Commands;
using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class VoidIncidentHandler : IRequestHandler<VoidIncidentCommand, IncidentDto>
{
    private readonly IIncidentRepository _incidents;
    private readonly IAuditRepository _audit;

    public VoidIncidentHandler(IIncidentRepository incidents, IAuditRepository audit)
    {
        _incidents = incidents;
        _audit = audit;
    }

    public async Task<IncidentDto> Handle(VoidIncidentCommand request, CancellationToken ct)
    {
        var incident = await _incidents.GetByIdAsync(request.IncidentId, ct)
            ?? throw new KeyNotFoundException($"Incident '{request.IncidentId}' not found.");

        incident.Void(request.Justification, request.ActorRole);
        await _incidents.UpdateAsync(incident, ct);

        await _audit.AppendAsync(AuditEvent.Create(
            "INCIDENT", incident.Id.ToString(), "VOIDED",
            request.ActorId, request.ActorName,
            $"{{\"justification\":\"{request.Justification}\"}}"), ct);

        return new IncidentDto(
            incident.Id, incident.IncidentNumber, incident.AircraftTailNumber,
            incident.FaultType.ToString(), incident.Severity.ToString(), incident.Status.ToString(),
            incident.Description, incident.StepsToReproduce, incident.TechnicianId, incident.SupervisorId,
            incident.CreatedBy, incident.CreatedAt, incident.UpdatedAt, incident.ResolvedAt, incident.ClosedAt,
            new List<AttachmentDto>());
    }
}
