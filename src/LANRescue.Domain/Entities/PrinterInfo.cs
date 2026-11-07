using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>Optional queue/driver information specific to printers.</summary>
public sealed record PrinterInfo
{
    public string? DriverName { get; init; }
    public string? PortName { get; init; }
    public string? PrintServer { get; init; }
    public string? ShareName { get; init; }
    public bool? IsShared { get; init; }
    public bool? IsPaused { get; init; }
    public PrinterOperationalStatus OperationalStatus { get; init; } = PrinterOperationalStatus.Unknown;
}
