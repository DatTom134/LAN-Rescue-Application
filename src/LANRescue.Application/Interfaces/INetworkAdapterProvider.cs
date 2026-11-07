using LANRescue.Domain.Models;

namespace LANRescue.Application.Interfaces;

public interface INetworkAdapterProvider
{
    Task<IReadOnlyList<NetworkAdapterSnapshot>> GetAdaptersAsync(
        CancellationToken cancellationToken = default
    );
}