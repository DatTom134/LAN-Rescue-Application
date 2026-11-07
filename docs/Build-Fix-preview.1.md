# Build fix — v0.3.0-preview.1

## Error

`App.xaml.cs`: `CS0118: 'Application' is a namespace but is used like a type`.

## Cause

The solution defines the namespace `LANRescue.Application` for the application layer.
In the WPF startup class, the unqualified identifier `Application` can resolve to that
namespace rather than `System.Windows.Application`.

## Fix

The WPF base class is now fully qualified:

```csharp
namespace LANRescue.ConsoleApp;

public partial class App : System.Windows.Application
{
}
```

This does not change the XAML class name, application-layer namespace, or runtime
behaviour. It only disambiguates the WPF base type.

## Verify

Run from the solution root:

```powershell
dotnet clean .\LANRescue.sln
dotnet restore .\LANRescue.sln
dotnet build .\LANRescue.sln
```
