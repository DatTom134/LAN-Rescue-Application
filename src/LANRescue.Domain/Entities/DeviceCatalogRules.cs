namespace LANRescue.Domain.Entities;

/// <summary>Validation rules that a repository/service must apply before saving a device.</summary>
public static class DeviceCatalogRules
{
    public static IReadOnlyList<string> Validate(
        ManagedDevice device,
        IEnumerable<ManagedDevice>? existingDevices = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(device.DeviceCode))
            errors.Add("DeviceCode is required.");
        else if (device.DeviceCode.Trim().Length > 64)
            errors.Add("DeviceCode must not exceed 64 characters.");

        if (string.IsNullOrWhiteSpace(device.DeviceName))
            errors.Add("DeviceName is required.");
        else if (device.DeviceName.Trim().Length > 253)
            errors.Add("DeviceName must not exceed 253 characters.");

        if (device.Id == Guid.Empty)
            errors.Add("Device Id must be a non-empty GUID.");

        if (device.Agent is not null && device.Agent.AgentId == Guid.Empty)
            errors.Add("AgentId must be a non-empty GUID when an Agent is registered.");

        if (existingDevices is not null)
        {
            var existing = existingDevices.ToArray();
            if (existing.Any(item => item.Id != device.Id && string.Equals(
                    item.DeviceCode?.Trim(), device.DeviceCode?.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors.Add($"DeviceCode '{device.DeviceCode}' is already in use.");

            if (device.Agent is not null && existing.Any(item => item.Id != device.Id &&
                    item.Agent?.AgentId == device.Agent.AgentId))
                errors.Add($"AgentId '{device.Agent.AgentId}' is already registered to another device.");
        }

        if (device.NetworkInterfaces.Count(nic => nic.IsPrimary) > 1)
            errors.Add("Only one network interface can be marked as primary.");

        foreach (var channel in device.ManagementChannels)
        {
            if (channel.Port is < 1 or > 65535)
                errors.Add($"Management port '{channel.Port}' must be between 1 and 65535.");
        }

        foreach (var nic in device.NetworkInterfaces)
        {
            foreach (var address in nic.Addresses)
            {
                if (address.PrefixLength is < 0 or > 128)
                    errors.Add($"Prefix length for address '{address.Address}' must be between 0 and 128.");
                if (!System.Net.IPAddress.TryParse(address.Address, out var parsedAddress))
                    errors.Add($"'{address.Address}' is not a valid IP address.");
                else if (address.PrefixLength is not null &&
                         parsedAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                         address.PrefixLength > 32)
                    errors.Add($"IPv4 prefix length for address '{address.Address}' cannot exceed 32.");
            }
        }

        return errors;
    }
}
