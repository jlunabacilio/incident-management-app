using IncidentReport.Application.Commands;
using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class CreateIncidentHandler : IRequestHandler<CreateIncidentCommand, IncidentDto>
{
    private readonly IIncidentRepository _incidents;
    private readonly IAircraftRepository _aircraft;
    private readonly IUserRepository _users;
    private readonly IAuditRepository _audit;
    private readonly IStorageService _storage;

    public CreateIncidentHandler(
        IIncidentRepository incidents,
        IAircraftRepository aircraft,
        IUserRepository users,
        IAuditRepository audit,
        IStorageService storage)
    {
        _incidents = incidents;
        _aircraft = aircraft;
        _users = users;
        _audit = audit;
        _storage = storage;
    }

    public async Task<IncidentDto> Handle(CreateIncidentCommand request, CancellationToken ct)
    {
        // Validate aircraft exists
        var aircraftEntity = await _aircraft.GetByTailNumberAsync(request.AircraftTailNumber, ct)
            ?? throw new KeyNotFoundException($"Aircraft '{request.AircraftTailNumber}' not found in registry.");

        // Validate technician and supervisor
        var technician = await _users.GetByIdAsync(request.TechnicianId, ct)
            ?? throw new KeyNotFoundException($"Technician '{request.TechnicianId}' not found.");
        var supervisor = await _users.GetByIdAsync(request.SupervisorId, ct)
            ?? throw new KeyNotFoundException($"Supervisor '{request.SupervisorId}' not found.");

        if (supervisor.Role < UserRole.Supervisor)
            throw new InvalidOperationException($"User '{request.SupervisorId}' does not have the Supervisor role.");

        // Validate attachment count before transaction
        if (request.Files.Count > 10)
            throw new InvalidOperationException("Maximum 10 attachments allowed per incident.");

        var seq = await _incidents.GetNextSequenceNumberAsync(ct);
        var incident = Incident.Create(
            request.AircraftTailNumber,
            request.FaultType,
            request.Severity,
            request.Description,
            request.StepsToReproduce,
            request.TechnicianId,
            request.SupervisorId,
            request.CreatedBy,
            seq);

        // Atomic: save incident + ground aircraft if CRITICAL
        await _incidents.AddAsync(incident, ct);

        if (request.Severity == Severity.Critical)
        {
            aircraftEntity.Ground(incident.Id, request.CreatedBy);
            await _aircraft.UpdateAsync(aircraftEntity, ct);

            await _audit.AppendAsync(AuditEvent.Create(
                "AIRCRAFT", aircraftEntity.TailNumber, "GROUNDED",
                request.CreatedBy, request.CreatedByName,
                $"{{\"incidentId\":\"{incident.Id}\",\"status\":\"GROUNDED\"}}"), ct);
        }

        await _audit.AppendAsync(AuditEvent.Create(
            "INCIDENT", incident.Id.ToString(), "CREATED",
            request.CreatedBy, request.CreatedByName,
            $"{{\"incidentNumber\":\"{incident.IncidentNumber}\",\"severity\":\"{request.Severity}\"}}"), ct);

        // Upload attachments post-commit (failure here does not roll back the incident)
        var attachmentDtos = new List<AttachmentDto>();
        foreach (var file in request.Files)
        {
            try
            {
                var key = await _storage.SaveAsync(incident.Id.ToString(), file.FileName, file.Content, file.MimeType, ct);
                var attachment = IncidentAttachment.Create(incident.Id, key, file.FileName, file.MimeType, file.SizeBytes, request.CreatedBy);
                incident.AddAttachment(attachment);
                await _incidents.UpdateAsync(incident, ct);
                var url = await _storage.GetUrlAsync(key, ct);
                attachmentDtos.Add(new AttachmentDto(attachment.Id, attachment.FileName, attachment.MimeType, attachment.SizeBytes, attachment.UploadedAt, attachment.UploadedBy, url));
            }
            catch (Exception)
            {
                // Log and continue — attachment failure does not roll back the incident
            }
        }

        return MapToDto(incident, attachmentDtos);
    }

    private static IncidentDto MapToDto(Incident incident, List<AttachmentDto> attachments) =>
        new(incident.Id, incident.IncidentNumber, incident.AircraftTailNumber,
            incident.FaultType.ToString(), incident.Severity.ToString(), incident.Status.ToString(),
            incident.Description, incident.StepsToReproduce, incident.TechnicianId, incident.SupervisorId,
            incident.CreatedBy, incident.CreatedAt, incident.UpdatedAt, incident.ResolvedAt, incident.ClosedAt,
            attachments);
}
