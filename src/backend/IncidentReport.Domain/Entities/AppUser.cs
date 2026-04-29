using IncidentReport.Domain.Enums;

namespace IncidentReport.Domain.Entities;

/// <summary>
/// Represents a system user (technician, supervisor, etc.).
/// </summary>
public class AppUser
{
    public string Id { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    // Required by EF Core
    private AppUser() { }

    public static AppUser Create(string id, string fullName, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new AppUser
        {
            Id = id,
            FullName = fullName,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }
}
