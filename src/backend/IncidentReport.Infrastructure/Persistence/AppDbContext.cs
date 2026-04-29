using IncidentReport.Domain.Entities;
using IncidentReport.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentReport.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentAttachment> IncidentAttachments => Set<IncidentAttachment>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Incident
        modelBuilder.Entity<Incident>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.IncidentNumber).IsRequired().HasMaxLength(20);
            e.Property(i => i.AircraftTailNumber).IsRequired().HasMaxLength(20);
            e.Property(i => i.Description).IsRequired().HasMaxLength(4000);
            e.Property(i => i.StepsToReproduce).IsRequired().HasMaxLength(2000);
            e.Property(i => i.FaultType).HasConversion<string>();
            e.Property(i => i.Severity).HasConversion<string>();
            e.Property(i => i.Status).HasConversion<string>();
            e.HasMany(i => i.Attachments).WithOne().HasForeignKey(a => a.IncidentId);
            e.HasIndex(i => i.AircraftTailNumber);
            e.HasIndex(i => i.Status);
            e.HasIndex(i => i.Severity);
            e.HasIndex(i => i.CreatedAt);
        });

        // IncidentAttachment
        modelBuilder.Entity<IncidentAttachment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.MimeType).IsRequired().HasMaxLength(50);
            e.Property(a => a.FileName).IsRequired().HasMaxLength(255);
            e.Property(a => a.StorageKey).IsRequired().HasMaxLength(500);
        });

        // Aircraft
        modelBuilder.Entity<Aircraft>(e =>
        {
            e.HasKey(a => a.TailNumber);
            e.Property(a => a.TailNumber).HasMaxLength(20);
            e.Property(a => a.OperationalStatus).HasConversion<string>();
        });

        // AuditEvent
        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            e.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
            e.Property(a => a.EventType).IsRequired().HasMaxLength(50);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
        });

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.Role).HasConversion<string>();
            e.HasIndex(u => u.Email).IsUnique();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed demo aircraft — use enum values directly, not strings
        modelBuilder.Entity<Aircraft>().HasData(
            new { TailNumber = "PP-XKA", Model = "Boeing 737-800", OperationalStatus = AircraftStatus.Available, GroundedByIncidentId = (Guid?)null, GroundedAt = (DateTime?)null, GroundedBy = (string?)null },
            new { TailNumber = "PR-GTA", Model = "Airbus A320",    OperationalStatus = AircraftStatus.Available, GroundedByIncidentId = (Guid?)null, GroundedAt = (DateTime?)null, GroundedBy = (string?)null },
            new { TailNumber = "PT-MXA", Model = "Embraer E175",   OperationalStatus = AircraftStatus.Available, GroundedByIncidentId = (Guid?)null, GroundedAt = (DateTime?)null, GroundedBy = (string?)null }
        );

        // Seed demo users — use enum values directly, not strings
        var demoHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
        modelBuilder.Entity<AppUser>().HasData(
            new { Id = "usr_tech_001",   FullName = "John Technician",  Email = "tech@demo.com",       PasswordHash = demoHash, Role = UserRole.Technician,    IsActive = true },
            new { Id = "usr_sup_001",    FullName = "Maria Supervisor",  Email = "supervisor@demo.com", PasswordHash = demoHash, Role = UserRole.Supervisor,    IsActive = true },
            new { Id = "usr_safety_001", FullName = "Carlos Safety",     Email = "safety@demo.com",     PasswordHash = demoHash, Role = UserRole.SafetyOfficer, IsActive = true }
        );
    }
}
