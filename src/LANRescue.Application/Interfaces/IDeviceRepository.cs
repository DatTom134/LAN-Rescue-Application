using LANRescue.Domain.Entities;

namespace LANRescue.Application.Interfaces;

/// <summary>Persistence contract for the device inventory.</summary>
public interface IDeviceRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedDevice>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ManagedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(ManagedDevice device, CancellationToken cancellationToken = default);
}
