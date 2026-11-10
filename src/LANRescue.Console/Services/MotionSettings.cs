using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace LANRescue.ConsoleApp.Services;

/// <summary>Per-user accessibility preference for non-essential motion.</summary>
public sealed class MotionSettings : INotifyPropertyChanged
{
    private static readonly Lazy<MotionSettings> Instance = new(() => new MotionSettings());
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LANRescue", "settings", "reduced-motion.txt");
    private bool _reducedMotion;

    public static MotionSettings Current => Instance.Value;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool ReducedMotion
    {
        get => _reducedMotion;
        set
        {
            if (_reducedMotion == value) return;
            _reducedMotion = value;
            Persist();
            OnPropertyChanged();
        }
    }

    private MotionSettings() { }

    public void Initialize()
    {
        try
        {
            if (File.Exists(_path) && bool.TryParse(File.ReadAllText(_path).Trim(), out var value))
                _reducedMotion = value;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void Persist()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, _reducedMotion.ToString());
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
