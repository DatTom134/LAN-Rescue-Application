# Build Fix – v0.4.0-preview.1 / preview.4

## Error

`Resources/Controls.xaml(89,61): error MC4111: Cannot find the Trigger target 'PressScale'.`

## Root cause

The button `ControlTemplate` assigned `x:Name="PressScale"` to a `ScaleTransform` nested inside `Grid.RenderTransform`. The `IsPressed` trigger then tried to address that `Freezable` by using `Setter.TargetName`. In this template context, WPF's template-name lookup did not resolve that transform as a valid named trigger target. The source order looked correct, but the target was not registered/resolvable in the way a named template element such as the `Grid` (`Root`) is.

## Fix

The trigger now targets the named `Grid` (`Root`), which is a real element in the `ControlTemplate` namescope, and replaces its `RenderTransform` with a pressed-state `ScaleTransform`:

```xml
<Trigger Property="IsPressed" Value="True">
    <Setter TargetName="Root" Property="RenderTransform">
        <Setter.Value>
            <ScaleTransform ScaleX="0.985" ScaleY="0.985"/>
        </Setter.Value>
    </Setter>
    <Setter TargetName="Chrome" Property="Opacity" Value="0.88"/>
</Trigger>
```

When `IsPressed` becomes false, WPF removes the trigger setter and the template's original identity `ScaleTransform` is restored. The button press-scale feedback is preserved without using `Setter.TargetName` on the transform object.

## Validation

- Confirmed the obsolete `PressScale` name/targets are no longer present in XAML.
- Confirmed the updated XAML is well-formed XML.
- The source environment does not have the .NET SDK or Windows WPF build tools, so a Windows `dotnet build` could not be executed here. Please run `dotnet build` on Windows to confirm the WPF markup compiler result.
