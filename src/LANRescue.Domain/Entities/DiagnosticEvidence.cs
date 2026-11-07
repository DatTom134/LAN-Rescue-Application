using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

public sealed record DiagnosticEvidence
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid SessionId { get; init; }

    public required string AdapterId { get; init; }

    public required string SourceAddress { get; init; }

    public required DiagnosticProbeType ProbeType { get; init; }

    public required DiagnosticEvidenceSource Source { get; init; }

    public required string Target { get; init; }

    public string? ResolverAddress { get; init; }

    public required DiagnosticProbeOutcome Outcome { get; init; }

    public bool IsSuccessful =>
        Outcome == DiagnosticProbeOutcome.Succeeded;

    public string? Details { get; init; }

    public long? DurationMs { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
        = DateTimeOffset.UtcNow;
}