using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Entities;
using IncidentReport.Domain.Enums;
using IncidentReport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentReport.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<AppUser?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<IEnumerable<AppUser>> GetByRoleAsync(UserRole role, CancellationToken ct = default) =>
        await _db.Users.Where(u => u.Role == role && u.IsActive).ToListAsync(ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }
}
