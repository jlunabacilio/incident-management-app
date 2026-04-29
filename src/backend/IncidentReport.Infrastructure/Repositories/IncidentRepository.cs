using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using IncidentReport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentReport.Infrastructure.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly AppDbContext _db;

    public IncidentRepository(AppDbContext db) => _db = db;

    public async Task<Incident?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Incidents.Include(i => i.Attachments).FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<(IEnumerable<Incident> Items, int Total)> SearchAsync(
        IncidentSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.Incidents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.TailNumber))
            query = query.Where(i => i.AircraftTailNumber == filter.TailNumber.ToUpperInvariant());
        if (filter.FaultType.HasValue)
            query = query.Where(i => i.FaultType == filter.FaultType.Value);
        if (filter.Severity.HasValue)
            query = query.Where(i => i.Severity == filter.Severity.Value);
        if (filter.Status.HasValue)
            query = query.Where(i => i.Status == filter.Status.Value);
        if (filter.DateFrom.HasValue)
            query = query.Where(i => i.CreatedAt >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            query = query.Where(i => i.CreatedAt <= filter.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(filter.TechnicianId))
            query = query.Where(i => i.TechnicianId == filter.TechnicianId);
        if (!string.IsNullOrWhiteSpace(filter.SupervisorId))
            query = query.Where(i => i.SupervisorId == filter.SupervisorId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> GetNextSequenceNumberAsync(CancellationToken ct = default) =>
        await _db.Incidents.CountAsync(ct) + 1;

    public async Task AddAsync(Incident incident, CancellationToken ct = default)
    {
        await _db.Incidents.AddAsync(incident, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Incident incident, CancellationToken ct = default)
    {
        _db.Incidents.Update(incident);
        await _db.SaveChangesAsync(ct);
    }
}
