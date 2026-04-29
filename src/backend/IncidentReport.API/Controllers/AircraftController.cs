using IncidentReport.Application.Commands;
using IncidentReport.Application.Interfaces;
using IncidentReport.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IncidentReport.API.Controllers;

/// <summary>Aircraft operational status and dispatch control.</summary>
[ApiController]
[Route("api/v1/aircraft")]
[Authorize]
public class AircraftController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAircraftRepository _aircraft;

    public AircraftController(IMediator mediator, IAircraftRepository aircraft)
    {
        _mediator = mediator;
        _aircraft = aircraft;
    }

    /// <summary>Returns the operational status of an aircraft.</summary>
    [HttpGet("{tailNumber}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStatus(string tailNumber, CancellationToken ct)
    {
        var aircraft = await _aircraft.GetByTailNumberAsync(tailNumber, ct);
        if (aircraft is null) return NotFound(new { error = $"Aircraft '{tailNumber}' not found." });

        return Ok(new
        {
            aircraft.TailNumber,
            Status = aircraft.OperationalStatus.ToString(),
            aircraft.GroundedByIncidentId,
            aircraft.GroundedAt
        });
    }

    /// <summary>Ungrounds an aircraft. Requires SafetyOfficer or ChiefEngineer role.</summary>
    [HttpPost("{tailNumber}/unground")]
    [Authorize(Roles = "SafetyOfficer,ChiefEngineer")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Unground(string tailNumber, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var actorName = User.FindFirstValue(ClaimTypes.Name)!;

        await _mediator.Send(new UngroundAircraftCommand(tailNumber, actorId, actorName), ct);
        return Ok(new { message = $"Aircraft '{tailNumber}' has been ungrounded." });
    }
}
