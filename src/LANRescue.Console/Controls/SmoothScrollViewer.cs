
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp.Controls;

/// <summary>
/// Frame-synchronized smooth scrolling for WPF.
/// Uses CompositionTarget.Rendering instead of a fixed DispatcherTimer.
/// </summary>
public sealed class SmoothScrollViewer : ScrollViewer
{
    // Time constant controls the smoothness of the movement.
    // Higher value = slower, softer movement.
    // Lower value = faster, more responsive movement.
    private const double ScrollTimeConstant = 0.105;

    // Stop animation when the remaining distance is sufficiently small.
    private const double SnapThreshold = 0.5;

    // Prevent unusually large time steps after the UI thread is paused.
    private const double MaxFrameDelta = 0.05;

    private double _targetOffset;
    private long _lastFrameTimestamp;
    private bool _isAnimating;

    public SmoothScrollViewer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MotionSettings.Current.PropertyChanged +=
            OnMotionSettingsChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        MotionSettings.Current.PropertyChanged -=
            OnMotionSettingsChanged;

        StopAnimation();
    }

    private void OnMotionSettingsChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MotionSettings.ReducedMotion) &&
            MotionSettings.Current.ReducedMotion)
        {
            StopAnimation();
        }
    }

    protected override void OnMouseWheel(
        System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Handled)
            return;

        if (MotionSettings.Current.ReducedMotion ||
            !IsLoaded ||
            ScrollableHeight <= 0)
        {
            StopAnimation();
            base.OnMouseWheel(e);
            return;
        }

        // Preserve high-resolution wheel input.
        double notches = e.Delta / 120.0;

        // Respect the user's Windows mouse wheel settings.
        int systemLines = SystemParameters.WheelScrollLines;

        double distance = systemLines < 0
            ? ViewportHeight
            : Math.Max(1, systemLines) * 36.0;

        double currentTarget = _isAnimating
            ? _targetOffset
            : VerticalOffset;

        _targetOffset = Math.Clamp(
            currentTarget - notches * distance,
            0,
            ScrollableHeight);

        StartAnimation();

        e.Handled = true;
    }

    private void StartAnimation()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        _lastFrameTimestamp = Stopwatch.GetTimestamp();

        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_isAnimating)
            return;

        if (!IsLoaded ||
            MotionSettings.Current.ReducedMotion)
        {
            StopAnimation();
            return;
        }

        long now = Stopwatch.GetTimestamp();

        double deltaTime = Stopwatch.GetElapsedTime(
            _lastFrameTimestamp).TotalSeconds;

        _lastFrameTimestamp = now;

        // Avoid large jumps after a rendering interruption.
        deltaTime = Math.Clamp(
            deltaTime,
            0.001,
            MaxFrameDelta);

        double currentOffset = VerticalOffset;

        double delta = _targetOffset - currentOffset;

        if (Math.Abs(delta) <= SnapThreshold)
        {
            ScrollToVerticalOffset(_targetOffset);
            StopAnimation();
            return;
        }

        // Frame-rate-independent exponential easing.
        double progress = 1.0 -
            Math.Exp(-deltaTime / ScrollTimeConstant);

        double nextOffset =
            currentOffset + delta * progress;

        ScrollToVerticalOffset(nextOffset);
    }

    private void StopAnimation()
    {
        if (_isAnimating)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        _isAnimating = false;
    }
}
