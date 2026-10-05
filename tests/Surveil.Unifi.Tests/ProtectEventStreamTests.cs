using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Events;
using Surveil.Infrastructure.Settings;
using Surveil.Unifi.WebSocket;

namespace Surveil.Unifi.Tests;

[TestFixture]
public sealed class ProtectEventStreamTests
{
    private static ProtectEventStream CreateStream(
        IWebSocketFactory webSocketFactory,
        string baseUrl = "https://host",
        string apiKey = "key",
        ISettingsChangeNotifier? notifier = null) =>
        new(TestFixtures.ProtectOptions(baseUrl, apiKey), webSocketFactory, TestFixtures.AllEventsEnabled(), notifier);

    [Test]
    public void BuildWebSocketUri_HttpsBaseUrl_UsesWss()
    {
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "https://nvr.example.invalid/proxy/protect/api");

        var uri = stream.BuildWebSocketUri();

        Assert.That(uri.Scheme, Is.EqualTo("wss"));
        Assert.That(uri.Host, Is.EqualTo("nvr.example.invalid"));
        Assert.That(uri.AbsolutePath.EndsWith("/v1/subscribe/events"), Is.True);
    }

    [Test]
    public void BuildWebSocketUri_HttpBaseUrl_UsesWs()
    {
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "http://nvr.example.invalid/api");

        var uri = stream.BuildWebSocketUri();

        Assert.That(uri.Scheme, Is.EqualTo("ws"));
    }

    [Test]
    public void BuildWebSocketUri_TrailingSlash_HandledCorrectly()
    {
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "https://host/api/");

        var uri = stream.BuildWebSocketUri();

        Assert.That(uri.Scheme, Is.EqualTo("wss"));
        Assert.That(uri.AbsolutePath.EndsWith("v1/subscribe/events"), Is.True);
    }

    [Test]
    public async Task SubscribeAsync_AlreadyCancelled_YieldsNoEvents()
    {
        var wsFactoryMock = new Mock<IWebSocketFactory>();
        var stream = CreateStream(wsFactoryMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.That(events, Is.Empty);
        wsFactoryMock.Verify(f => f.Create(It.IsAny<string>()), Times.Never());
    }

    [Test]
    public async Task SubscribeAsync_ConnectFails_RetriesAfterTheBackoffDelay()
    {
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.SetupGet(w => w.State).Returns(WebSocketState.Closed);
        wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
              .Returns(Task.FromException(new WebSocketException("refused")));

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(wsMock.Object);

        var stream = CreateStream(wsFactoryMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.That(events, Is.Empty);
        wsFactoryMock.Verify(f => f.Create(It.IsAny<string>()), Times.AtLeast(2));
    }

    [Test]
    public async Task SubscribeAsync_ConnectCancelledImmediately_YieldsNoEvents()
    {
        using var cts = new CancellationTokenSource();

        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
              .Returns((Uri _, CancellationToken ct) =>
              {
                  cts.Cancel();
                  return Task.FromCanceled(ct);
              });

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(wsMock.Object);

        var stream = CreateStream(wsFactoryMock.Object);

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.That(events, Is.Empty);
    }

    [Test]
    public async Task SubscribeAsync_ConnectsAndReceivesEvent_YieldsEvent()
    {
        const string json = """{"type":"add","item":{"id":"ev1","type":"ring","start":1000,"device":"dev1"}}""";
        var stream = CreateStream(TestFixtures.WebSocketDelivering(json));
        using var cts = new CancellationTokenSource(5000);

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
        {
            events.Add(e);
            break;
        }

        Assert.That(events, Has.Exactly(1).Items);
        Assert.That(events[0].Id, Is.EqualTo("ev1"));
        Assert.That(events[0].DeviceId, Is.EqualTo("dev1"));
        Assert.That(events[0].Description, Is.EqualTo("Doorbell ring"));
    }

    [Test]
    public async Task SubscribeAsync_ReceivesCloseMessage_ReconnectsWithBackoff()
    {
        var receiveCount = 0;
        var wsMock = new Mock<IWebSocketConnection>();
        wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        wsMock.SetupGet(w => w.State).Returns(WebSocketState.Open);
        wsMock.Setup(w => w.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
              .Returns(() =>
              {
                  receiveCount++;
                  return new ValueTask<ValueWebSocketReceiveResult>(
                      new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
              });

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(wsMock.Object);

        var stream = CreateStream(wsFactoryMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));

        var events = new List<CameraEvent>();
        await foreach (var e in stream.SubscribeAsync(cts.Token))
            events.Add(e);

        Assert.That(events, Is.Empty);
        Assert.That(receiveCount > 0, Is.True, "At least one receive call was made");
        wsFactoryMock.Verify(f => f.Create(It.IsAny<string>()), Times.AtLeast(2));
    }

    [Test]
    public void SettingsChanged_UpdatesBuildWebSocketUri()
    {
        var notifier = new SettingsChangeNotifier();
        var stream = CreateStream(new Mock<IWebSocketFactory>().Object, "https://host1", "key1", notifier);

        Assert.That(stream.BuildWebSocketUri().Host, Is.EqualTo("host1"));

        notifier.NotifyChanged(new AppSettings
        {
            SelectedProvider = VideoProviderType.UnifiProtect,
            UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://host2", ApiKey = "key2" }
        });

        Assert.That(stream.BuildWebSocketUri().Host, Is.EqualTo("host2"));
    }

    [Test]
    public async Task SettingsChanged_DuringConnect_AbortsAndReconnectsWithNewApiKey()
    {
        var notifier = new SettingsChangeNotifier();
        var capturedApiKeys = new List<string>();
        var firstConnectStarted = new TaskCompletionSource();

        var wsFactoryMock = new Mock<IWebSocketFactory>();
        wsFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns((string apiKey) =>
        {
            capturedApiKeys.Add(apiKey);
            return capturedApiKeys.Count == 1 ? ConnectHangsUntilAborted() : ConnectsThenClosesImmediately();
        });

        var stream = CreateStream(wsFactoryMock.Object, apiKey: "old-key", notifier: notifier);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var readTask = Task.Run(async () =>
        {
            await foreach (var _ in stream.SubscribeAsync(cts.Token)) { }
        });

        await firstConnectStarted.Task;

        notifier.NotifyChanged(new AppSettings
        {
            SelectedProvider = VideoProviderType.UnifiProtect,
            UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://host", ApiKey = "new-key" }
        });

        await readTask;

        Assert.That(capturedApiKeys, Does.Contain("new-key"));

        IWebSocketConnection ConnectHangsUntilAborted()
        {
            var wsMock = new Mock<IWebSocketConnection>();
            wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                  .Returns((Uri _, CancellationToken ct) =>
                  {
                      firstConnectStarted.TrySetResult();
                      return Task.Delay(Timeout.Infinite, ct);
                  });
            return wsMock.Object;
        }

        static IWebSocketConnection ConnectsThenClosesImmediately()
        {
            var wsMock = new Mock<IWebSocketConnection>();
            wsMock.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            wsMock.SetupGet(w => w.State).Returns(WebSocketState.Open);
            wsMock.Setup(w => w.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                  .Returns(new ValueTask<ValueWebSocketReceiveResult>(
                      new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true)));
            return wsMock.Object;
        }
    }
}
