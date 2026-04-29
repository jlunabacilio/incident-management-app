using IncidentReport.Domain.Entities;
using MediatR;

namespace IncidentReport.Application.Queries;

public record GetAuditTrailQuery(string EntityType, string EntityId) : IRequest<IEnumerable<AuditEvent>>;
