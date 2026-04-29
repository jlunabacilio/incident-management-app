using IncidentReport.Application.Commands;
using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class UpdateIncidentHandler : IRequestHandler<UpdateIncidentCommand, IncidentDto>
{
    private readonly IIncidentRepository _incidents;
    private readonly IAuditRepository _audit;

    public UpdateIncidentHandler(IIncidentRepository incidents, IAuditRepository audit)
    {
        _incidents = incidents;
        _audit = audit;
    }

    public async Task<IncidentDto> Handle(UpdateIncidentCommand request, CancellationToken ct)
    {
        var incident = await _incidents.GetByIdAsync(request.IncidentId, ct)
            ?? throw new KeyNotFoundException($"Incident '{request.IncidentId}' not found.");

        incident.Update(request.Description, request.StepsToReproduce, request.TechnicianId, request.SupervisorId);
        await _incidents.UpdateAsync(incident, ct);

        await _audit.AppendAsync(AuditEvent.Create(
            "INCIDENT", incident.Id.ToString(), "UPDATED",
            request.ActorId, request.ActorName,
            $"{{\"description\":\"{request.Description[..Math.Min(50, request.Description.Length)]}...\"}}"), ct);

        return new IncidentDto(
            incident.Id, incident.IncidentNumber, incident.AircraftTailNumber,
            incident.FaultType.ToString(), incident.Severity.ToString(), incident.Status.ToString(),
            incident.Description, incident.StepsToReproduce, incident.TechnicianId, incident.SupervisorId,
            incident.CreatedBy, incident.CreatedAt, incident.UpdatedAt, incident.ResolvedAt, incident.ClosedAt,
            new List<AttachmentDto>());
    }
}
