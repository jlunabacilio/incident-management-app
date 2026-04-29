using IncidentReport.Domain.Enums;

namespace IncidentReport.Domain.Entities;

/// <summary>
/// Represents a maintenance incident registered against an aircraft.
/// </summary>
public class Incident
{
    public Guid Id { get; private set; }
    public string IncidentNumber { get; private set; } = string.Empty;
    public string AircraftTailNumber { get; private set; } = string.Empty;
    public FaultType FaultType { get; private set; }
    public Severity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string StepsToReproduce { get; private set; } = string.Empty;
    public string TechnicianId { get; private set; } = string.Empty;
    public string SupervisorId { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? VoidJustification { get; private set; }

    // Navigation
    public ICollection<IncidentAttachment> Attachments { get; private set; } = new List<IncidentAttachment>();

    // Required by EF Core
    private Incident() { }

    public static Incident Create(
        string aircraftTailNumber,
        FaultType faultType,
        Severity severity,
        string description,
        string stepsToReproduce,
        string technicianId,
        string supervisorId,
        string createdBy,
        int sequenceNumber)
    {
        if (string.IsNullOrWhiteSpace(aircraftTailNumber))
            throw new ArgumentException("Aircraft tail number is required.", nameof(aircraftTailNumber));
        if (description.Length < 20 || description.Length > 4000)
            throw new ArgumentException("Description must be between 20 and 4000 characters.", nameof(description));
        if (stepsToReproduce.Length < 10 || stepsToReproduce.Length > 2000)
            throw new ArgumentException("Steps to reproduce must be between 10 and 2000 characters.", nameof(stepsToReproduce));

        var now = DateTime.UtcNow;
        return new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = $"INC-{now.Year}-{sequenceNumber:D6}",
            AircraftTailNumber = aircraftTailNumber.ToUpperInvariant(),
            FaultType = faultType,
            Severity = severity,
            Status = IncidentStatus.Open,
            Description = description,
            StepsToReproduce = stepsToReproduce,
            TechnicianId = technicianId,
            SupervisorId = supervisorId,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void TransitionTo(IncidentStatus newStatus, string actorId, UserRole actorRole)
    {
        IncidentStateMachine.ValidateTransition(Status, newStatus, actorRole);
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

        if (newStatus == IncidentStatus.Resolved)
            ResolvedAt = DateTime.UtcNow;
        if (newStatus == IncidentStatus.Closed)
            ClosedAt = DateTime.UtcNow;
    }

    public void Update(string description, string stepsToReproduce, string technicianId, string supervisorId)
    {
        if (Status != IncidentStatus.Open && Status != IncidentStatus.InProgress)
            throw new InvalidOperationException("Incident can only be updated when Open or In Progress.");
        if (description.Length < 20 || description.Length > 4000)
            throw new ArgumentException("Description must be between 20 and 4000 characters.", nameof(description));
        if (stepsToReproduce.Length < 10 || stepsToReproduce.Length > 2000)
            throw new ArgumentException("Steps to reproduce must be between 10 and 2000 characters.", nameof(stepsToReproduce));

        Description = description;
        StepsToReproduce = stepsToReproduce;
        TechnicianId = technicianId;
        SupervisorId = supervisorId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Void(string justification, UserRole actorRole)
    {
        if (actorRole < UserRole.Supervisor)
            throw new UnauthorizedAccessException("Only Supervisors or higher can void an incident.");
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Justification is required to void an incident.", nameof(justification));

        Status = IncidentStatus.Voided;
        VoidJustification = justification;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAttachment(IncidentAttachment attachment)
    {
        if (Attachments.Count >= 10)
            throw new InvalidOperationException("Maximum of 10 attachments allowed per incident.");
        Attachments.Add(attachment);
    }

    public void RemoveAttachment(Guid attachmentId)
    {
        if (Status != IncidentStatus.Open)
            throw new InvalidOperationException("Attachments can only be removed when the incident is Open.");
        var attachment = Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new KeyNotFoundException($"Attachment {attachmentId} not found.");
        Attachments.Remove(attachment);
    }
}
