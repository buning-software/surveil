using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WinUia;

namespace Surveil.UiTests.Application;

public sealed class SurveilApp : App
{
    private const string DataDirectoryVariable = "SURVEIL_DATA_DIR";
    private const string ExePathVariable = "SURVEIL_EXE";

    private static readonly TimeSpan WindowAbsentTimeout = TimeSpan.FromSeconds(1);

    public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), $"surveil-ui-tests-{Guid.NewGuid():N}");

    public string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public string? TryReadSettingsFile()
    {
        try
        {
            return File.Exists(SettingsFile) ? File.ReadAllText(SettingsFile) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    protected override string ExecutablePath
    {
        get
        {
            var path = Environment.GetEnvironmentVariable(ExePathVariable) is { Length: > 0 } overridden
                ? overridden
                : typeof(SurveilApp).Assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Single(a => a.Key == "SurveilExePath")
                    .Value;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException(
                    $"Surveil.exe not found at '{path}'. Build src/Surveil for the same configuration and platform first, or set {ExePathVariable}.");

            return path;
        }
    }

    protected override AppLaunchOptions DefaultOptions => new()
    {
        ShowPointer = true,
        Environment = new Dictionary<string, string?> { [DataDirectoryVariable] = DataDirectory }
    };

    public Layout Shell => field ??= new Layout(MainWindow);

    public bool IsMainWindowShown => TryFindWindow("Surveil", WindowAbsentTimeout) is not null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Close(TimeSpan.Zero);

        base.Dispose(disposing);

        if (disposing && Directory.Exists(DataDirectory))
            Directory.Delete(DataDirectory, recursive: true);
    }
}
