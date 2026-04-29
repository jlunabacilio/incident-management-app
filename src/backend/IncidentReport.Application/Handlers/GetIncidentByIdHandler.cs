using IncidentReport.Application.DTOs;
using IncidentReport.Application.Interfaces;
using IncidentReport.Application.Queries;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class GetIncidentByIdHandler : IRequestHandler<GetIncidentByIdQuery, IncidentDto?>
{
    private readonly IIncidentRepository _incidents;
    private readonly IStorageService _storage;

    public GetIncidentByIdHandler(IIncidentRepository incidents, IStorageService storage)
    {
        _incidents = incidents;
        _storage = storage;
    }

    public async Task<IncidentDto?> Handle(GetIncidentByIdQuery request, CancellationToken ct)
    {
        var incident = await _incidents.GetByIdAsync(request.Id, ct);
        if (incident is null) return null;

        var attachmentDtos = new List<AttachmentDto>();
        foreach (var a in incident.Attachments)
        {
            var url = await _storage.GetUrlAsync(a.StorageKey, ct);
            attachmentDtos.Add(new AttachmentDto(a.Id, a.FileName, a.MimeType, a.SizeBytes, a.UploadedAt, a.UploadedBy, url));
        }

        return new IncidentDto(
            incident.Id, incident.IncidentNumber, incident.AircraftTailNumber,
            incident.FaultType.ToString(), incident.Severity.ToString(), incident.Status.ToString(),
            incident.Description, incident.StepsToReproduce, incident.TechnicianId, incident.SupervisorId,
            incident.CreatedBy, incident.CreatedAt, incident.UpdatedAt, incident.ResolvedAt, incident.ClosedAt,
            attachmentDtos);
    }
}
