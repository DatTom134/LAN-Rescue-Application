# Build fix — preview.3

## Issues addressed

1. `CS0103: The name 'Path' does not exist in the current context` in `DevicesView.xaml.cs`.
   - Added `using System.IO;` for `Path.Combine`.

2. `CS8602` nullable dereference warnings in `DeviceEditorWindow.xaml.cs`.
   - Replaced the repeated conditional access comparison with a nullable `matchingOldIpv4` reference.
   - The previous IPv4 health and observation timestamp are retained only when the entered IPv4 matches the existing one. Otherwise, health defaults to `Unknown` and the observation timestamp is cleared.

## Validation

- Checked changed C# source text and project references.
- Checked all XAML and project XML files parse successfully.
- A .NET SDK is not available in the packaging environment, so `dotnet build` has not been executed here. Build verification must be performed on the Windows development machine.
