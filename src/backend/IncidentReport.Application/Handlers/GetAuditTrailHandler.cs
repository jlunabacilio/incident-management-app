using IncidentReport.Application.Interfaces;
using IncidentReport.Application.Queries;
using IncidentReport.Domain.Entities;
using MediatR;

namespace IncidentReport.Application.Handlers;

public class GetAuditTrailHandler : IRequestHandler<GetAuditTrailQuery, IEnumerable<AuditEvent>>
{
    private readonly IAuditRepository _audit;

    public GetAuditTrailHandler(IAuditRepository audit) => _audit = audit;

    public Task<IEnumerable<AuditEvent>> Handle(GetAuditTrailQuery request, CancellationToken ct) =>
        _audit.GetByEntityAsync(request.EntityType, request.EntityId, ct);
}
