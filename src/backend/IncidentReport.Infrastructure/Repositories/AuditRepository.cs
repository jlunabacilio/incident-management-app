using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using IncidentReport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentReport.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly AppDbContext _db;

    public AuditRepository(AppDbContext db) => _db = db;

    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        await _db.AuditEvents.AddAsync(auditEvent, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AuditEvent>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct = default) =>
        await _db.AuditEvents
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(ct);
}
