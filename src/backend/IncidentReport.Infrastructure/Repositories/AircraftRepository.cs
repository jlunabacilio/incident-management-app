using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using IncidentReport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentReport.Infrastructure.Repositories;

public class AircraftRepository : IAircraftRepository
{
    private readonly AppDbContext _db;

    public AircraftRepository(AppDbContext db) => _db = db;

    public async Task<Aircraft?> GetByTailNumberAsync(string tailNumber, CancellationToken ct = default) =>
        await _db.Aircraft.FirstOrDefaultAsync(a => a.TailNumber == tailNumber.ToUpperInvariant(), ct);

    public async Task UpdateAsync(Aircraft aircraft, CancellationToken ct = default)
    {
        _db.Aircraft.Update(aircraft);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Aircraft>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Aircraft.ToListAsync(ct);
}
