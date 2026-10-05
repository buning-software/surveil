using NUnit.Framework;
using Surveil.Application.Ports;
using Surveil.Infrastructure.Startup;
using Windows.ApplicationModel;

namespace Surveil.Core.Tests.Infrastructure;

[TestFixture]
public sealed class WindowsStartupTaskServiceTests
{
    [TestCase(StartupTaskState.Enabled, StartupTaskStatus.Enabled)]
    [TestCase(StartupTaskState.EnabledByPolicy, StartupTaskStatus.EnabledByPolicy)]
    [TestCase(StartupTaskState.Disabled, StartupTaskStatus.Disabled)]
    [TestCase(StartupTaskState.DisabledByUser, StartupTaskStatus.DisabledByUser)]
    [TestCase(StartupTaskState.DisabledByPolicy, StartupTaskStatus.DisabledByPolicy)]
    public void Map_TranslatesEveryKnownWindowsState(StartupTaskState state, StartupTaskStatus expected)
    {
        Assert.That(WindowsStartupTaskService.Map(state), Is.EqualTo(expected));
    }

    [Test]
    public void Map_ForAnUnknownState_FallsBackToUnavailable()
    {
        Assert.That(WindowsStartupTaskService.Map((StartupTaskState)999), Is.EqualTo(StartupTaskStatus.Unavailable));
    }

    [Test]
    public void StartupTaskId_MatchesThePackageManifest()
    {
        Assert.That(WindowsStartupTaskService.StartupTaskId, Is.EqualTo("SurveilStartupTask"));
    }
}
