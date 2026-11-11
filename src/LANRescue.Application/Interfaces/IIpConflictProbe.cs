using System.Net;
using LANRescue.Domain.Entities;

namespace LANRescue.Application.Interfaces;

public interface IIpConflictProbe
{
    Task<IReadOnlyList<IpAddressObservation>> ScanSubnetAsync(
        string adapterId,
        string adapterName,
        IPAddress sourceAddress,
        IPAddress subnetMask,
        CancellationToken cancellationToken = default);
}