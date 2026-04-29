using IncidentReport.Domain.Entities;
using IncidentReport.Domain.Enums;

namespace IncidentReport.Application.Interfaces;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Incident> Items, int Total)> SearchAsync(IncidentSearchFilter filter, CancellationToken ct = default);
    Task<int> GetNextSequenceNumberAsync(CancellationToken ct = default);
    Task AddAsync(Incident incident, CancellationToken ct = default);
    Task UpdateAsync(Incident incident, CancellationToken ct = default);
}

public record IncidentSearchFilter(
    string? TailNumber,
    FaultType? FaultType,
    Severity? Severity,
    IncidentStatus? Status,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? TechnicianId,
    string? SupervisorId,
    int Page = 1,
    int PageSize = 20);
