using IncidentReport.Application.Interfaces;
using IncidentReport.Infrastructure.Auth;
using IncidentReport.Infrastructure.Persistence;
using IncidentReport.Infrastructure.Repositories;
using IncidentReport.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core with SQLite
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=incident.db"));

        // Repositories
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IAircraftRepository, AircraftRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Storage
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5000";
        services.AddSingleton<IStorageService>(new LocalFileStorageService(uploadsPath, baseUrl));

        // Auth
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
