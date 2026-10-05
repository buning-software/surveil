using NUnit.Framework;
using Surveil.UiTests.Application;
using WinUia;
using static WinUia.NUnit.UiAssertions;

namespace Surveil.UiTests.Tests;

[TestFixture]
public sealed class MainWindowTests
{
    private SurveilApp _app = null!;

    [SetUp]
    public void SetUp() => _app = App.Launch<SurveilApp>();

    [TearDown]
    public void TearDown() => _app.Dispose();

    [Test]
    public void Launch_ShowsWindowTitledSurveil()
    {
        Assert.That(_app.MainWindow.Name, Is.EqualTo("Surveil"));
    }

    [Test]
    public void Launch_ShowsCamerasNavigationItem()
    {
        Assert.That(_app.Shell.CamerasItem.Name, Is.EqualTo("Cameras"));
    }

    [Test]
    public void Launch_ShowsSettingsNavigationItem()
    {
        Assert.That(_app.Shell.SettingsItem.IsEnabled, Is.True);
    }

    [Test]
    public void CloseButton_HidesWindowAndKeepsRunningInTray()
    {
        Assert.That(_app.IsMainWindowShown, Is.True);

        _app.Shell.CloseButton.Click();

        Eventually(() => !_app.IsMainWindowShown, "closing the window hides it to the tray");
        Assert.That(_app.Process.HasExited, Is.False);
    }
}
