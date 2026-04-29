using IncidentReport.Domain.Enums;

namespace IncidentReport.Application.DTOs;

public record IncidentDto(
    Guid Id,
    string IncidentNumber,
    string AircraftTailNumber,
    string FaultType,
    string Severity,
    string Status,
    string Description,
    string StepsToReproduce,
    string TechnicianId,
    string SupervisorId,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    List<AttachmentDto> Attachments
);

public record AttachmentDto(
    Guid Id,
    string FileName,
    string MimeType,
    long SizeBytes,
    DateTime UploadedAt,
    string UploadedBy,
    string? Url
);

public record IncidentSummaryDto(
    Guid Id,
    string IncidentNumber,
    string AircraftTailNumber,
    string FaultType,
    string Severity,
    string Status,
    string TechnicianId,
    DateTime CreatedAt
);

public record DashboardSummaryDto(
    int TotalOpen,
    int LowCount,
    int MediumCount,
    int HighCount,
    int CriticalCount,
    List<string> GroundedAircraft
);
