using IncidentReport.Domain.Enums;

namespace IncidentReport.Domain.Entities;

/// <summary>
/// Enforces valid state transitions and role permissions for incident lifecycle.
/// </summary>
public static class IncidentStateMachine
{
    private static readonly Dictionary<(IncidentStatus From, IncidentStatus To), UserRole> AllowedTransitions = new()
    {
        { (IncidentStatus.Open,        IncidentStatus.InProgress),    UserRole.Technician },
        { (IncidentStatus.InProgress,  IncidentStatus.PendingReview), UserRole.Technician },
        { (IncidentStatus.PendingReview, IncidentStatus.Resolved),    UserRole.Supervisor },
        { (IncidentStatus.Resolved,    IncidentStatus.Closed),        UserRole.SafetyOfficer },
        { (IncidentStatus.Open,        IncidentStatus.Voided),        UserRole.Supervisor },
        { (IncidentStatus.InProgress,  IncidentStatus.Voided),        UserRole.Supervisor },
    };

    public static void ValidateTransition(IncidentStatus from, IncidentStatus to, UserRole actorRole)
    {
        if (!AllowedTransitions.TryGetValue((from, to), out var requiredRole))
            throw new InvalidOperationException($"Transition from {from} to {to} is not allowed.");

        if (actorRole < requiredRole)
            throw new UnauthorizedAccessException(
                $"Role {actorRole} is not authorized to transition from {from} to {to}. Required: {requiredRole}.");
    }

    public static IEnumerable<IncidentStatus> GetAllowedTransitions(IncidentStatus current, UserRole actorRole)
    {
        return AllowedTransitions
            .Where(kv => kv.Key.From == current && actorRole >= kv.Value)
            .Select(kv => kv.Key.To);
    }
}
