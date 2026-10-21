using System;
using NUnit.Framework;
using Surveil.UiTests.Application;
using static WinUia.NUnit.UiAssertions;

namespace Surveil.UiTests.Tests;

public sealed class MainWindowTests : SurveilFixture
{
    [Test]
    public void Launch_ShowsWindowTitledSurveil()
    {
        Assert.That(App.MainWindow.Name, Is.EqualTo("Surveil"));
    }

    [Test]
    public void Launch_OpensWindowWithinScreen()
    {
        if (App.MainWindow.Parent is not { } desktop)
        {
            Assert.Fail("The main window has no desktop parent.");
            return;
        }

        var window = App.MainWindow.BoundingRectangle;
        var screen = desktop.BoundingRectangle;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(window.Left, Is.GreaterThanOrEqualTo(screen.Left));
            Assert.That(window.Top, Is.GreaterThanOrEqualTo(screen.Top));
            Assert.That(window.Right, Is.LessThanOrEqualTo(screen.Right));
            Assert.That(window.Bottom, Is.LessThanOrEqualTo(screen.Bottom));
        }
    }

    [Test]
    public void Launch_ShowsCamerasNavigationItem()
    {
        Assert.That(App.CamerasItem.Name, Is.EqualTo("Cameras"));
    }

    [Test]
    public void Launch_ShowsSettingsNavigationItem()
    {
        Assert.That(App.SettingsItem.IsEnabled, Is.True);
    }

    [Test]
    public void CloseButton_HidesWindowAndKeepsRunningInTray()
    {
        Assert.That(App.IsMainWindowShown, Is.True);

        App.CloseButton.Click();

        Eventually(() => !App.IsMainWindowShown, "closing the window hides it to the tray");
        Assert.That(App.Process.HasExited, Is.False);
    }
}
