using LANRescue.Domain.Entities;

namespace LANRescue.Application.Interfaces;

public interface IDiagnosticRepository
{
    Task SaveSessionAsync(
        DiagnosticSession session,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<DiagnosticSession>> GetRecentSessionsAsync(
        int limit,
        CancellationToken cancellationToken = default
    );
}