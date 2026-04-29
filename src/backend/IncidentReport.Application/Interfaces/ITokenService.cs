using IncidentReport.Domain.Entities;

namespace IncidentReport.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(AppUser user);
}
