using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace LANRescue.ConsoleApp.Services;

/// <summary>
/// Requests native Windows 11 Mica / Desktop Acrylic backdrops and rounded corners.
/// XAML surface brushes provide the fallback when DWM materials are unavailable.
/// </summary>
public static class WindowBackdrop
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwcpRound = 2;
    private const int DwmSbtMainWindow = 2;
    private const int DwmSbtTransientWindow = 3;

    public static void Apply(Window window, AppTheme theme, bool transient = false)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        try
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                SetAttribute(handle, DwmwaUseImmersiveDarkMode, theme == AppTheme.Dark ? 1 : 0);
                SetAttribute(handle, DwmwaWindowCornerPreference, DwmwcpRound);
            }

            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
                return;

            // Enable transparent WPF rendering only on supported Windows 11 builds.
            window.Background = Brushes.Transparent;

            // WPF normally paints an opaque composition target. Make the target transparent
            // and extend the DWM frame through the client area so the requested material can show.
            var source = HwndSource.FromHwnd(handle);
            if (source?.CompositionTarget is { } compositionTarget)
                compositionTarget.BackgroundColor = Colors.Transparent;

            var margins = new DwmMargins
            {
                Left = -1,
                Right = -1,
                Top = -1,
                Bottom = -1
            };
            _ = DwmExtendFrameIntoClientArea(handle, ref margins);
            SetAttribute(handle, DwmwaSystemBackdropType,
                transient ? DwmSbtTransientWindow : DwmSbtMainWindow);
        }
        catch (DllNotFoundException)
        {
            // DWM is unavailable; fall back to regular WPF surfaces.
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows builds may not expose every DWM function/attribute.
        }
    }

    private static void SetAttribute(IntPtr hwnd, int attribute, int value)
    {
        _ = DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DwmMargins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref DwmMargins margins);
}
