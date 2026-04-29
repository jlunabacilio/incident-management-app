using IncidentReport.Domain.Entities;

namespace IncidentReport.Application.Interfaces;

public interface IAircraftRepository
{
    Task<Aircraft?> GetByTailNumberAsync(string tailNumber, CancellationToken ct = default);
    Task UpdateAsync(Aircraft aircraft, CancellationToken ct = default);
    Task<IEnumerable<Aircraft>> GetAllAsync(CancellationToken ct = default);
}
