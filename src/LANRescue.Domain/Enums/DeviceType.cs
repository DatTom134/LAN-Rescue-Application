namespace LANRescue.Domain.Enums;

/// <summary>Broad categories supported by the inventory. Device-specific details live in separate objects.</summary>
public enum DeviceType
{
    Computer,
    Printer,
    Server,
    Switch,
    Router,
    Firewall,
    AccessPoint,
    Other
}
