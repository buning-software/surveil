using System;
using System.Collections.Generic;
using System.IO;
using WinUia;
using WinUia.Models;

namespace Surveil.UiTests.Application;

public sealed class TestDataDirectory : IDisposable
{
    private const string DataDirectoryVariable = "SURVEIL_DATA_DIR";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"surveil-ui-tests-{Guid.NewGuid():N}");

    public string SettingsFile => Path.Combine(_root, "settings.json");

    public AppLaunchOptions LaunchOptions => new()
    {
        Environment = new Dictionary<string, string?> { [DataDirectoryVariable] = _root }
    };

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

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
