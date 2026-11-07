using System.Management;
using System.Runtime.InteropServices;

namespace LANRescue.Infrastructure.Network;

internal sealed record WindowsAdapterMetadata(
    bool? HardwareInterface,
    bool? IsHidden);

internal static class WindowsAdapterMetadataReader
{
    public static IReadOnlyDictionary<string, WindowsAdapterMetadata> Read()
    {
        var result =
            new Dictionary<string, WindowsAdapterMetadata>(
                StringComparer.OrdinalIgnoreCase);

        if (!OperatingSystem.IsWindows())
            return result;

        try
        {
            var scope = new ManagementScope(
                @"\\.\root\StandardCimv2");

            scope.Connect();

            var query = new ObjectQuery(
                "SELECT InterfaceGuid, HardwareInterface, Hidden " +
                "FROM MSFT_NetAdapter");

            using var searcher = new ManagementObjectSearcher(
                scope,
                query);

            using var collection = searcher.Get();

            foreach (ManagementObject item in collection)
            {
                using (item)
                {
                    var interfaceGuid =
                        item["InterfaceGuid"]?.ToString();

                    if (string.IsNullOrWhiteSpace(interfaceGuid))
                        continue;

                    if (!Guid.TryParse(interfaceGuid, out var guid))
                        continue;

                    var hardwareInterface =
                        item["HardwareInterface"] is bool hardware
                            ? hardware
                            : (bool?)null;

                    var isHidden =
                        item["Hidden"] is bool hidden
                            ? hidden
                            : (bool?)null;

                    result[guid.ToString("D")] =
                        new WindowsAdapterMetadata(
                            hardwareInterface,
                            isHidden);
                }
            }
        }
        catch (ManagementException)
        {
            // WMI không truy vấn được.
        }
        catch (COMException)
        {
            // Lỗi giao tiếp với Windows Management.
        }
        catch (UnauthorizedAccessException)
        {
            // Không đủ quyền truy vấn metadata.
        }

        return result;
    }

    public static string NormalizeGuid(string value)
    {
        return Guid.TryParse(value, out var guid)
            ? guid.ToString("D")
            : value.Trim();
    }
}