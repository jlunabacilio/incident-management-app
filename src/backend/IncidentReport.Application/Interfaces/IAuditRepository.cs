using IncidentReport.Domain.Entities;

namespace IncidentReport.Application.Interfaces;

public interface IAuditRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken ct = default);
    Task<IEnumerable<AuditEvent>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default);
}
