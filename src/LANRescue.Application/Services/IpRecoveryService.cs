
using LANRescue.Application.Services;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using LANRescue.Application.Models;

namespace LANRescue.Application.Services;

/// <summary>
/// Provides read-only target discovery for the IP Recovery workflow.
/// Actual remote network changes are not implemented in this stage.
/// </summary>
public sealed class IpRecoveryService
{
    private readonly DeviceCatalogService _deviceCatalog;
    private readonly IpRecoveryPlanValidator _planValidator = new();

    public IpRecoveryService(DeviceCatalogService deviceCatalog)
    {
        _deviceCatalog = deviceCatalog
            ?? throw new ArgumentNullException(nameof(deviceCatalog));
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await _deviceCatalog.InitializeAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagedDevice>> GetEligibleTargetsAsync(
        CancellationToken cancellationToken = default)
    {
        var devices = await _deviceCatalog.GetAllAsync(cancellationToken);

        return devices
            .Where(device => device.IsManaged)
            .Where(device => !device.IsSampleData)
            .Where(device => device.Type == DeviceType.Computer)
            .OrderBy(device => device.DeviceName)
            .ToArray();
    }

    public async Task<ManagedDevice?> GetTargetAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceCatalog.GetByIdAsync(
            deviceId,
            cancellationToken);

        if (device is null ||
            !device.IsManaged ||
            device.IsSampleData ||
            device.Type != DeviceType.Computer)
        {
            return null;
        }

        return device;
    }

    public IpRecoveryValidationResult ValidatePlan(
        IpNetworkConfiguration desired,
        IpNetworkConfiguration? current = null)
    {
        return _planValidator.Validate(desired, current);
    }
}
