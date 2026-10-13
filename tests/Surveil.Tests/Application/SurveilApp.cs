using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WinUia;
using WinUia.Models;

namespace Surveil.UiTests.Application;

public sealed class SurveilApp : App
{
    private const string ExePathVariable = "SURVEIL_EXE";
    private const string ExeName = "Surveil.exe";
    private const string CaptionControlsClassName = "ReunionWindowingCaptionControls";

    public bool IsMainWindowShown => TryFindWindow("Surveil", TimeSpan.FromSeconds(1)) is not null;

    public Element NavigationView => MainWindow.FindByAutomationId("NavView");

    public Element CamerasItem => MainWindow.FindByAutomationId("CamerasGroup");

    public Element SettingsItem => MainWindow.FindByAutomationId("SettingsItem");

    public Element CloseButton => MainWindow
        .Find(e => e.ClassName == CaptionControlsClassName, TreeScope.Children)
        .FindByAutomationId("Close");

    protected override string ExecutablePath => Environment.GetEnvironmentVariable(ExePathVariable) is { Length: > 0 } overridden
        ? overridden
        : FindBuiltExecutable();

    protected override AppLaunchOptions DefaultOptions => new() { ShowPointer = true };

    public SettingsPage OpenSettings()
    {
        SettingsItem.Click();
        return new SettingsPage(MainWindow);
    }

    private static string FindBuiltExecutable()
    {
        var binDirectory = typeof(SurveilApp).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "SurveilBinDirectory")
            .Value ?? string.Empty;

        var executables = Directory.Exists(binDirectory)
            ? Directory.EnumerateFiles(binDirectory, ExeName, SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}AppX{Path.DirectorySeparatorChar}"))
                .ToArray()
            : [];

        return executables.Length == 1
            ? executables[0]
            : throw new FileNotFoundException(
                $"Expected exactly one {ExeName} under '{binDirectory}' but found {executables.Length}. Build src/Surveil for the same configuration and platform first, or set {ExePathVariable}.");
    }
}
