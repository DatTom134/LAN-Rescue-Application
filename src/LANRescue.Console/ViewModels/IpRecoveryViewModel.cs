using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LANRescue.Domain.Entities;

namespace LANRescue.ConsoleApp.ViewModels;

public sealed class IpRecoveryViewModel
{
    public IReadOnlyList<ManagedDevice> Devices { get; init; }
        = Array.Empty<ManagedDevice>();

    public ManagedDevice? SelectedDevice { get; set; }

    public NetworkInterfaceInfo? SelectedInterface { get; set; }

    public IpNetworkConfiguration CurrentConfiguration { get; set; }
        = new();

    public bool IsTargetVerified { get; set; }

    public bool CanRecover { get; set; }

    public string ManagementStatus { get; set; } = "";
}
