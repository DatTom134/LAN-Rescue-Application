using LANRescue.Domain.Entities;

namespace LANRescue.Application.Interfaces;

public interface IIpRecoveryService
{
    Task<IReadOnlyList<ManagedDevice>> GetEligibleTargetsAsync(
        CancellationToken cancellationToken = default);

    Task<ManagedDevice?> GetTargetAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}