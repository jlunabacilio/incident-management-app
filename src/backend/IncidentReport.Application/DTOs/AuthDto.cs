namespace IncidentReport.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string UserId,
    string FullName,
    string Role,
    DateTime ExpiresAt
);
