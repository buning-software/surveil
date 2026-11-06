using System;
using NUnit.Framework;
using Surveil.UiTests.Application;
using WinUia;

namespace Surveil.UiTests.Tests;

public abstract class SurveilFixture
{
    protected TestDataDirectory Data { get; private set; } = null!;

    protected SurveilApp App { get; private set; } = null!;

    [SetUp]
    public void LaunchApp()
    {
        Data = new TestDataDirectory();
        App = WinUia.App.Launch<SurveilApp>(Data.LaunchOptions);
    }

    [TearDown]
    public void CloseApp()
    {
        App.Close(TimeSpan.Zero);
        App.Dispose();
        Data.Dispose();
    }

    protected static void Eventually(Func<bool> condition, string because = "") =>
        Assert.That(Poll.Until(condition), Is.True, because);
}
