namespace IncidentReport.Domain.Entities;

/// <summary>
/// Immutable audit record for every state change on incidents and aircraft.
/// </summary>
public class AuditEvent
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string ActorName { get; private set; } = string.Empty;
    public string? PreviousValue { get; private set; }
    public string NewValue { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    // Required by EF Core
    private AuditEvent() { }

    public static AuditEvent Create(
        string entityType,
        string entityId,
        string eventType,
        string actorId,
        string actorName,
        string newValue,
        string? previousValue = null)
    {
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            ActorId = actorId,
            ActorName = actorName,
            PreviousValue = previousValue,
            NewValue = newValue,
            OccurredAt = DateTime.UtcNow
        };
    }
}
