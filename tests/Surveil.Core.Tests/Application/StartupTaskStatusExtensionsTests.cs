using System.Collections.Generic;
using NUnit.Framework;
using Surveil.Application.Ports;

namespace Surveil.Core.Tests.Application;

[TestFixture]
public sealed class StartupTaskStatusExtensionsTests
{
    [TestCase(StartupTaskStatus.Enabled)]
    [TestCase(StartupTaskStatus.EnabledByPolicy)]
    public void IsEnabled_ForAnEnabledState_IsTrue(StartupTaskStatus status)
    {
        Assert.That(status.IsEnabled(), Is.True);
    }

    [TestCase(StartupTaskStatus.Disabled)]
    [TestCase(StartupTaskStatus.DisabledByUser)]
    [TestCase(StartupTaskStatus.DisabledByPolicy)]
    [TestCase(StartupTaskStatus.Unavailable)]
    public void IsEnabled_ForEveryOtherState_IsFalse(StartupTaskStatus status)
    {
        Assert.That(status.IsEnabled(), Is.False);
    }

    [TestCase(StartupTaskStatus.Enabled)]
    [TestCase(StartupTaskStatus.Disabled)]
    public void CanUserChange_WhenWindowsLeavesTheDecisionToTheApp_IsTrue(StartupTaskStatus status)
    {
        Assert.That(status.CanUserChange(), Is.True);
    }

    [TestCase(StartupTaskStatus.DisabledByUser)]
    [TestCase(StartupTaskStatus.DisabledByPolicy)]
    [TestCase(StartupTaskStatus.EnabledByPolicy)]
    [TestCase(StartupTaskStatus.Unavailable)]
    public void CanUserChange_WhenTheDecisionIsTakenElsewhere_IsFalse(StartupTaskStatus status)
    {
        Assert.That(status.CanUserChange(), Is.False);
    }

    [TestCase(StartupTaskStatus.Enabled)]
    [TestCase(StartupTaskStatus.Disabled)]
    public void GetRestrictionDescription_WhenTheSettingIsChangeable_IsNull(StartupTaskStatus status)
    {
        Assert.That(status.GetRestrictionDescription(), Is.Null);
    }

    [TestCase(StartupTaskStatus.DisabledByUser)]
    [TestCase(StartupTaskStatus.DisabledByPolicy)]
    [TestCase(StartupTaskStatus.EnabledByPolicy)]
    [TestCase(StartupTaskStatus.Unavailable)]
    public void GetRestrictionDescription_WhenTheSettingIsLocked_ExplainsWhy(StartupTaskStatus status)
    {
        var description = status.GetRestrictionDescription();

        Assert.That(description, Is.Not.Null);
        Assert.That(string.IsNullOrWhiteSpace(description), Is.False);
    }

    [Test]
    public void GetRestrictionDescription_ForEveryLockedStatus_IsDistinct()
    {
        StartupTaskStatus[] locked =
        [
            StartupTaskStatus.DisabledByUser,
            StartupTaskStatus.DisabledByPolicy,
            StartupTaskStatus.EnabledByPolicy,
            StartupTaskStatus.Unavailable
        ];

        var descriptions = new HashSet<string>();

        foreach (var status in locked)
        {
            var description = status.GetRestrictionDescription();

            Assert.That(description, Is.Not.Null);
            Assert.That(descriptions.Add(description), Is.True, $"{status} reuses another status' description.");
        }
    }
}
