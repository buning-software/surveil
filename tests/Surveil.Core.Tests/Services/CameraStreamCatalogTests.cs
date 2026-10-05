using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.Infrastructure.Settings;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
public sealed class CameraStreamCatalogTests
{
    private const string CameraId = "cam-1";

    private static readonly RtspsStream High = new("rtsp://host/high", "high");
    private static readonly RtspsStream Low = new("rtsp://host/low", "low");

    private readonly Mock<ICameraProvider> _provider = new();
    private readonly SettingsChangeNotifier _settingsNotifier = new();

    private CameraStreamCatalog CreateCatalog() => new(_provider.Object, _settingsNotifier);

    private void SetupExisting(params RtspsStream[] streams) =>
        _provider
            .Setup(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streams);

    private void SetupCreated(params RtspsStream[] streams) =>
        _provider
            .Setup(p => p.CreateRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streams);

    [Test]
    public async Task GetStreamsAsync_SeveralExistingQualities_ReturnsThemWithoutCreating()
    {
        SetupExisting(High, Low);
        using var catalog = CreateCatalog();

        var streams = await catalog.GetStreamsAsync(CameraId);

        Assert.That(streams.ToArray(), Is.EqualTo(new[] { High, Low }));
        _provider.Verify(p => p.CreateRtspsStreamsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetStreamsAsync_SingleExistingQuality_ReturnsCreatedStreams()
    {
        SetupExisting(High);
        SetupCreated(High, Low);
        using var catalog = CreateCatalog();

        var streams = await catalog.GetStreamsAsync(CameraId);

        Assert.That(streams.ToArray(), Is.EqualTo(new[] { High, Low }));
    }

    [Test]
    public async Task GetStreamsAsync_NoExistingStreams_ReturnsCreatedStreams()
    {
        SetupExisting();
        SetupCreated(High, Low);
        using var catalog = CreateCatalog();

        var streams = await catalog.GetStreamsAsync(CameraId);

        Assert.That(streams, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetStreamsAsync_CreateFailsWithExistingStream_ReturnsExisting()
    {
        SetupExisting(High);
        _provider
            .Setup(p => p.CreateRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unsupported"));
        using var catalog = CreateCatalog();

        var streams = await catalog.GetStreamsAsync(CameraId);

        Assert.That(streams, Has.Exactly(1).Items);
        Assert.That(streams[0], Is.EqualTo(High));
    }

    [Test]
    public async Task GetStreamsAsync_CreateFailsWithoutExistingStream_Throws()
    {
        SetupExisting();
        _provider
            .Setup(p => p.CreateRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unsupported"));
        using var catalog = CreateCatalog();

        await Assert.CatchAsync<InvalidOperationException>(() => catalog.GetStreamsAsync(CameraId));
    }

    [Test]
    public async Task GetStreamsAsync_CalledTwice_FetchesOnce()
    {
        SetupExisting(High, Low);
        using var catalog = CreateCatalog();

        await catalog.GetStreamsAsync(CameraId);
        await catalog.GetStreamsAsync(CameraId);

        _provider.Verify(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetStreamsAsync_ConcurrentCalls_ShareOneFetch()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<RtspsStream>>();
        _provider
            .Setup(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        using var catalog = CreateCatalog();

        var first = catalog.GetStreamsAsync(CameraId);
        var second = catalog.GetStreamsAsync(CameraId);
        pending.SetResult([High, Low]);
        await Task.WhenAll(first, second);

        _provider.Verify(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetStreamsAsync_AfterAFailure_FetchesAgain()
    {
        _provider
            .SetupSequence(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("offline"))
            .ReturnsAsync([High, Low]);
        using var catalog = CreateCatalog();

        await Assert.CatchAsync<InvalidOperationException>(() => catalog.GetStreamsAsync(CameraId));
        var streams = await catalog.GetStreamsAsync(CameraId);

        Assert.That(streams, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetStreamsAsync_CallerCancels_DoesNotCancelTheSharedFetch()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<RtspsStream>>();
        _provider
            .Setup(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        using var catalog = CreateCatalog();
        using var cts = new CancellationTokenSource();

        var cancelled = catalog.GetStreamsAsync(CameraId, cts.Token);
        cts.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(() => cancelled);

        var second = catalog.GetStreamsAsync(CameraId);
        pending.SetResult([High, Low]);

        Assert.That(await second, Has.Count.EqualTo(2));
        _provider.Verify(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SettingsChanged_ClearsTheCache()
    {
        SetupExisting(High, Low);
        using var catalog = CreateCatalog();
        await catalog.GetStreamsAsync(CameraId);

        _settingsNotifier.NotifyChanged(new AppSettings { SelectedProvider = VideoProviderType.UnifiProtect });
        await catalog.GetStreamsAsync(CameraId);

        _provider.Verify(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task Prefetch_StartsTheFetchSoALaterGetIsServedFromCache()
    {
        SetupExisting(High, Low);
        using var catalog = CreateCatalog();

        catalog.Prefetch([CameraId]);
        await catalog.GetStreamsAsync(CameraId);

        _provider.Verify(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Prefetch_FetchFails_DoesNotThrow()
    {
        _provider
            .Setup(p => p.GetRtspsStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("offline"));
        using var catalog = CreateCatalog();

        catalog.Prefetch([CameraId]);
    }
}
