using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;

namespace LANRescue.Application.Services;

/// <summary>Application use cases for creating, editing and retiring inventory records.</summary>
public sealed class DeviceCatalogService(IDeviceRepository repository)
{
    private readonly IDeviceRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _repository.InitializeAsync(cancellationToken);

    public Task<IReadOnlyList<ManagedDevice>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<ManagedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public async Task<ManagedDevice> SaveAsync(
        ManagedDevice device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var normalized = device with
        {
            DeviceCode = device.DeviceCode?.Trim() ?? string.Empty,
            DeviceName = device.DeviceName?.Trim() ?? string.Empty,
            Manufacturer = Normalize(device.Manufacturer),
            Model = Normalize(device.Model),
            SerialNumber = Normalize(device.SerialNumber),
            FirmwareVersion = Normalize(device.FirmwareVersion),
            Department = Normalize(device.Department),
            Location = Normalize(device.Location),
            Notes = Normalize(device.Notes),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var existing = await _repository.GetAllAsync(cancellationToken);
        var errors = DeviceCatalogRules.Validate(normalized, existing);
        if (errors.Count > 0)
            throw new DeviceValidationException(errors);

        await _repository.SaveAsync(normalized, cancellationToken);
        return normalized;
    }

    public async Task<ManagedDevice> SetManagedAsync(
        Guid id,
        bool isManaged,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Device '{id}' was not found.");

        var updated = existing with
        {
            IsManaged = isManaged,
            DeactivatedAt = isManaged ? null : DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await SaveAsync(updated, cancellationToken);
        return updated;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
