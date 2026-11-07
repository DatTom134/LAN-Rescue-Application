# Build fix — v0.2.0-preview.2

## Cause

`DeviceRecord` exposes a string presentation property named `DeviceType` for WPF binding. Inside the same class, references such as `DeviceType.Computer` were therefore resolved against that property name instead of the enum type `LANRescue.Domain.Enums.DeviceType`. This caused CS1061 and CS0120 errors.

## Change

The presentation property name and WPF bindings are preserved for compatibility. `DeviceRecord.cs` now declares the alias:

```csharp
using DeviceTypeEnum = LANRescue.Domain.Enums.DeviceType;
```

All enum type references inside `DeviceRecord` use `DeviceTypeEnum`, including the Computer, Server, AccessPoint and Firewall members and the `GetDeviceTypeLabel` parameter.

## Validation

Static inspection confirms that the ambiguous enum references in `DeviceRecord.cs` have been replaced and that the WPF bindings to `DeviceType` remain unchanged. A .NET SDK is not installed in the packaging environment, so a real `dotnet build` could not be executed here. Run the build commands in the repository README on Windows.
