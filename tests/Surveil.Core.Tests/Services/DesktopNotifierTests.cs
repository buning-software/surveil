using System;
using NUnit.Framework;
using Moq;
using Surveil.Application.Settings;
using Surveil.Domain.Events;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
public sealed class DesktopNotifierTests
{
    private const string MissingSnapshotPath = @"C:\nonexistent\path\snapshot.jpg";

    private readonly Mock<IAppNotificationSender> _sender = new();
    private readonly CameraEvent _event = new("id", "dev1", "Doorbell ring");

    private DesktopNotifier CreateNotifier() =>
        new(new SnapshotOptions(MissingSnapshotPath), _sender.Object);

    [Test]
    public void Notify_WhenTheHeroSnapshotDoesNotExist_SendsTheTitleWithoutAHero()
    {
        var notifier = CreateNotifier();

        notifier.Notify(_event, "Front Door");

        _sender.Verify(s => s.Notify("Doorbell ring (Front Door)", null), Times.Once());
    }

    [Test]
    public void Notify_WhenTheSenderThrows_SuppressesTheException()
    {
        _sender.Setup(s => s.Notify(It.IsAny<string>(), It.IsAny<string?>()))
               .Throws(new InvalidOperationException("WinRT not initialized"));
        var notifier = CreateNotifier();

        notifier.Notify(_event, "Front Door");

        _sender.Verify(s => s.Notify("Doorbell ring (Front Door)", null), Times.Once());
    }
}
