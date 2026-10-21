using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Surveil.Domain.Cameras;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
public sealed class SnapshotGrabberTests
{
    private const string CameraId = "cam-1";
    private const string HighUrl = "rtsp://host/high";
    private const string LowUrl = "rtsp://host/low";

    private static readonly TimeSpan LongTimeout = TimeSpan.FromSeconds(10);

    private sealed class TestVlcHandle(int framesOnPlay) : IVlcPlayerHandle
    {
        public event EventHandler? Playing;
        public event EventHandler? EncounteredError;
        public event EventHandler? EndReached;
        public event EventHandler<VideoFrame>? FrameReady;

        public bool IsDisposed { get; private set; }

        public void Play()
        {
            Playing?.Invoke(this, EventArgs.Empty);
            for (var i = 0; i < framesOnPlay; i++)
                FrameReady?.Invoke(this, CreateFrame(4 + i, 4));
        }

        public void Stop() { }
        public void Dispose() => IsDisposed = true;

        public void RaiseError() => EncounteredError?.Invoke(this, EventArgs.Empty);
        public void RaiseEnd() => EndReached?.Invoke(this, EventArgs.Empty);
    }

    private sealed class TestVlcFactory(int framesOnPlay) : IVlcPlayerFactory
    {
        public ConcurrentQueue<TestVlcHandle> Handles { get; } = new();
        public ConcurrentQueue<string> Urls { get; } = new();

        public IVlcPlayerHandle Create(string url, Action<string> onError)
        {
            var handle = new TestVlcHandle(framesOnPlay);
            Urls.Enqueue(url);
            Handles.Enqueue(handle);
            return handle;
        }
    }

    private sealed record SavedFrame(int Width, int Height, int Length);

    private readonly Mock<ICameraStreamCatalog> _catalog = new();
    private readonly List<SavedFrame> _saved = [];

    public SnapshotGrabberTests()
    {
        _catalog
            .Setup(c => c.GetStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RtspsStream(HighUrl, "high"), new RtspsStream(LowUrl, "low")]);
    }

    private static VideoFrame CreateFrame(int width, int height)
    {
        var dataLength = width * height * 4;
        return new VideoFrame(ArrayPool<byte>.Shared.Rent(dataLength), width, height, dataLength);
    }

    private Task Save(int width, int height, byte[] pixels)
    {
        lock (_saved)
            _saved.Add(new SavedFrame(width, height, pixels.Length));
        return Task.CompletedTask;
    }

    private SnapshotGrabber CreateGrabber(TestVlcFactory factory, TimeSpan? timeout = null, Func<int, int, byte[], Task>? save = null) =>
        new(_catalog.Object, factory, save ?? Save, timeout ?? LongTimeout);

    [Test]
    public async Task GrabAsync_FirstFrame_SavesFrameAndReturnsTrue()
    {
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory);

        var result = await grabber.GrabAsync(CameraId);

        Assert.That(result, Is.True);
        Assert.That(_saved, Has.Exactly(1).Items);
        Assert.That(_saved[0], Is.EqualTo(new SavedFrame(4, 4, 4 * 4 * 4)));
    }

    [Test]
    public async Task GrabAsync_FirstFrame_DisposesHandle()
    {
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory);

        await grabber.GrabAsync(CameraId);

        Assert.That(factory.Handles.TryPeek(out var handle), Is.True);
        Assert.That(handle?.IsDisposed, Is.True);
    }

    [Test]
    public async Task GrabAsync_SeveralFrames_SavesOnlyTheFirst()
    {
        var factory = new TestVlcFactory(framesOnPlay: 3);
        using var grabber = CreateGrabber(factory);

        await grabber.GrabAsync(CameraId);

        Assert.That(_saved, Has.Exactly(1).Items);
        Assert.That(_saved[0].Width, Is.EqualTo(4));
    }

    [Test]
    public async Task GrabAsync_SeveralQualities_UsesTheLowestStream()
    {
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory);

        await grabber.GrabAsync(CameraId);

        Assert.That(factory.Urls.TryPeek(out var url), Is.True);
        Assert.That(url, Is.EqualTo(LowUrl));
    }

    [Test]
    public async Task GrabAsync_SingleQuality_UsesIt()
    {
        _catalog
            .Setup(c => c.GetStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RtspsStream(HighUrl, "high")]);
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory);

        await grabber.GrabAsync(CameraId);

        Assert.That(factory.Urls.TryPeek(out var url), Is.True);
        Assert.That(url, Is.EqualTo(HighUrl));
    }

    [Test]
    public async Task GrabAsync_NoFrameBeforeTimeout_ReturnsFalseAndDisposesHandle()
    {
        var factory = new TestVlcFactory(framesOnPlay: 0);
        using var grabber = CreateGrabber(factory, TimeSpan.FromMilliseconds(50));

        var result = await grabber.GrabAsync(CameraId);

        Assert.That(result, Is.False);
        Assert.That(_saved, Is.Empty);
        Assert.That(factory.Handles.TryPeek(out var handle), Is.True);
        Assert.That(handle?.IsDisposed, Is.True);
    }

    [Test]
    public async Task GrabAsync_Cancelled_ThrowsAndDisposesHandle()
    {
        var factory = new TestVlcFactory(framesOnPlay: 0);
        using var grabber = CreateGrabber(factory);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.CatchAsync<OperationCanceledException>(() => grabber.GrabAsync(CameraId, cts.Token));

        Assert.That(factory.Handles.TryPeek(out var handle), Is.True);
        Assert.That(handle?.IsDisposed, Is.True);
    }

    [Test]
    public async Task GrabAsync_CatalogThrows_ReturnsFalse()
    {
        _catalog
            .Setup(c => c.GetStreamsAsync(CameraId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("offline"));
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory);

        var result = await grabber.GrabAsync(CameraId);

        Assert.That(result, Is.False);
        Assert.That(factory.Handles, Is.Empty);
    }

    [Test]
    public async Task GrabAsync_SaveThrows_ReturnsFalse()
    {
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory, save: (_, _, _) => throw new InvalidOperationException("disk full"));

        var result = await grabber.GrabAsync(CameraId);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GrabAsync_CalledConcurrently_RunsOneAtATime()
    {
        var releaseFirstSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saveCount = 0;
        var factory = new TestVlcFactory(framesOnPlay: 1);
        using var grabber = CreateGrabber(factory, save: async (_, _, _) =>
        {
            if (Interlocked.Increment(ref saveCount) != 1) return;
            firstSaveStarted.SetResult();
            await releaseFirstSave.Task;
        });

        var first = grabber.GrabAsync(CameraId);
        await firstSaveStarted.Task.WaitAsync(LongTimeout);
        var second = grabber.GrabAsync(CameraId);
        await Task.Delay(50);

        Assert.That(factory.Handles, Has.Count.EqualTo(1));

        releaseFirstSave.SetResult();
        await Task.WhenAll(first, second).WaitAsync(LongTimeout);

        Assert.That(factory.Handles, Has.Count.EqualTo(2));
        Assert.That(saveCount, Is.EqualTo(2));
    }
}
