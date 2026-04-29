using IncidentReport.Application.DTOs;
using IncidentReport.Domain.Enums;
using MediatR;

namespace IncidentReport.Application.Commands;

public record CreateIncidentCommand(
    string AircraftTailNumber,
    FaultType FaultType,
    Severity Severity,
    string Description,
    string StepsToReproduce,
    string TechnicianId,
    string SupervisorId,
    string CreatedBy,
    string CreatedByName,
    List<FileUploadItem> Files
) : IRequest<IncidentDto>;

public record FileUploadItem(string FileName, string MimeType, long SizeBytes, Stream Content);
