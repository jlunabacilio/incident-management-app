using IncidentReport.Domain.Entities;
using IncidentReport.Domain.Enums;

namespace IncidentReport.Application.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<AppUser>> GetByRoleAsync(UserRole role, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
}
