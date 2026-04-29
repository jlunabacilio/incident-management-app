using IncidentReport.Domain.Enums;

namespace IncidentReport.Domain.Entities;

/// <summary>
/// Represents an aircraft in the registry, including its operational status.
/// </summary>
public class Aircraft
{
    public string TailNumber { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public AircraftStatus OperationalStatus { get; private set; }
    public Guid? GroundedByIncidentId { get; private set; }
    public DateTime? GroundedAt { get; private set; }
    public string? GroundedBy { get; private set; }

    // Required by EF Core
    private Aircraft() { }

    public static Aircraft Create(string tailNumber, string model)
    {
        if (string.IsNullOrWhiteSpace(tailNumber))
            throw new ArgumentException("Tail number is required.", nameof(tailNumber));

        return new Aircraft
        {
            TailNumber = tailNumber.ToUpperInvariant(),
            Model = model,
            OperationalStatus = AircraftStatus.Available
        };
    }

    public void Ground(Guid incidentId, string groundedBy)
    {
        OperationalStatus = AircraftStatus.Grounded;
        GroundedByIncidentId = incidentId;
        GroundedAt = DateTime.UtcNow;
        GroundedBy = groundedBy;
    }

    public void Unground()
    {
        OperationalStatus = AircraftStatus.Available;
        GroundedByIncidentId = null;
        GroundedAt = null;
        GroundedBy = null;
    }
}
