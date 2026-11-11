using LANRescue.Domain.Entities;

namespace LANRescue.Application.Interfaces;

public interface IIpConflictRepository
{
    Task InitializeAsync(
        CancellationToken cancellationToken = default);

    Task SaveObservationsAsync(
        IReadOnlyList<IpAddressObservation> observations,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IpAddressObservation>> GetRecentObservationsAsync(
        string adapterId,
        string sourceAddress,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default);
}