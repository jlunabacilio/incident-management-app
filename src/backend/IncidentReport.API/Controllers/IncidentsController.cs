using IncidentReport.Application.Commands;
using IncidentReport.Application.Queries;
using IncidentReport.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IncidentReport.API.Controllers;

/// <summary>Maintenance incident registration and lifecycle management.</summary>
[ApiController]
[Route("api/v1/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IncidentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Creates a new maintenance incident. Accepts multipart/form-data with optional image attachments.</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create([FromForm] CreateIncidentRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<FaultType>(request.FaultType, true, out var faultType))
            return UnprocessableEntity(new { error = "VALIDATION_ERROR", details = new[] { new { field = "faultType", message = "Invalid fault type." } } });
        if (!Enum.TryParse<Severity>(request.Severity, true, out var severity))
            return UnprocessableEntity(new { error = "VALIDATION_ERROR", details = new[] { new { field = "severity", message = "Invalid severity." } } });

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var actorName = User.FindFirstValue(ClaimTypes.Name)!;

        var files = (request.Attachments ?? []).Select(f =>
            new FileUploadItem(f.FileName, f.ContentType, f.Length, f.OpenReadStream())).ToList();

        var command = new CreateIncidentCommand(
            request.AircraftTailNumber, faultType, severity,
            request.Description, request.StepsToReproduce,
            request.TechnicianId, request.SupervisorId,
            actorId, actorName, files);

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Returns a paginated list of incidents with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tailNumber,
        [FromQuery] string? faultType,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? technicianId,
        [FromQuery] string? supervisorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Enum.TryParse<FaultType>(faultType, true, out var ft);
        Enum.TryParse<Severity>(severity, true, out var sv);
        Enum.TryParse<IncidentStatus>(status, true, out var st);

        var query = new GetIncidentsQuery(
            tailNumber,
            string.IsNullOrEmpty(faultType) ? null : ft,
            string.IsNullOrEmpty(severity) ? null : sv,
            string.IsNullOrEmpty(status) ? null : st,
            dateFrom, dateTo, technicianId, supervisorId, page, pageSize);

        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>Returns the full detail of a single incident including attachments.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetIncidentByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Updates editable fields of an incident (allowed when Open or In Progress).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIncidentRequest request, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var actorName = User.FindFirstValue(ClaimTypes.Name)!;

        var command = new UpdateIncidentCommand(id, request.Description, request.StepsToReproduce,
            request.TechnicianId, request.SupervisorId, actorId, actorName);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Transitions an incident to a new status. Role requirements depend on the target status.</summary>
    [HttpPost("{id:guid}/transition")]
    [Authorize(Roles = "Supervisor,MaintenanceControl,SafetyOfficer,ChiefEngineer,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Transition(Guid id, [FromBody] TransitionRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<IncidentStatus>(request.NewStatus, true, out var newStatus))
            return BadRequest(new { error = "Invalid status value." });

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var actorName = User.FindFirstValue(ClaimTypes.Name)!;
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), true, out var actorRole);

        var command = new TransitionIncidentCommand(id, newStatus, actorId, actorName, actorRole);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Voids an incident with a mandatory justification. Requires Supervisor role or higher.</summary>
    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Supervisor,MaintenanceControl,SafetyOfficer,ChiefEngineer,Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidRequest request, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var actorName = User.FindFirstValue(ClaimTypes.Name)!;
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), true, out var actorRole);

        var command = new VoidIncidentCommand(id, request.Justification, actorId, actorName, actorRole);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Returns the audit trail for a specific incident.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Roles = "MaintenanceControl,SafetyOfficer,ChiefEngineer,Admin")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAudit(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAuditTrailQuery("INCIDENT", id.ToString()), ct);
        return Ok(result);
    }

    /// <summary>Returns a dashboard summary of open incidents grouped by severity.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }
}

// Request models
public record CreateIncidentRequest(
    string AircraftTailNumber,
    string FaultType,
    string Severity,
    string Description,
    string StepsToReproduce,
    string TechnicianId,
    string SupervisorId,
    List<IFormFile>? Attachments
);

public record UpdateIncidentRequest(
    string Description,
    string StepsToReproduce,
    string TechnicianId,
    string SupervisorId
);

public record TransitionRequest(string NewStatus);
public record VoidRequest(string Justification);
