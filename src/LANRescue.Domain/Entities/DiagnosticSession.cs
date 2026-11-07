using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

public sealed record DiagnosticSession
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string AdapterId { get; init; }

    public required string AdapterName { get; init; }

    public DiagnosticStatus OverallStatus { get; init; }
        = DiagnosticStatus.Unknown;

    public DateTimeOffset StartedAt { get; init; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; init; }

    public IReadOnlyList<DiagnosticEvidence> Evidence { get; init; }
        = Array.Empty<DiagnosticEvidence>();
}