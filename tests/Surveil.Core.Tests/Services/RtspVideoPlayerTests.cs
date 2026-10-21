using System;
using System.Buffers;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class RtspVideoPlayerTests
{
    private sealed class TestVlcHandle : IVlcPlayerHandle
    {
        public event EventHandler? Playing;
        public event EventHandler? EncounteredError;
        public event EventHandler? EndReached;
        public event EventHandler<VideoFrame>? FrameReady;

        public int PlayCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Play() => PlayCallCount++;
        public void Stop() => StopCallCount++;
        public void Dispose() => IsDisposed = true;

        public void RaisePlaying() => Playing?.Invoke(this, EventArgs.Empty);
        public void RaiseEncounteredError() => EncounteredError?.Invoke(this, EventArgs.Empty);
        public void RaiseEndReached() => EndReached?.Invoke(this, EventArgs.Empty);
        public void RaiseFrameReady(VideoFrame frame) => FrameReady?.Invoke(this, frame);
    }

    private sealed class TestVlcFactory(TestVlcHandle handle) : IVlcPlayerFactory
    {
        public Action<string>? CapturedOnError { get; private set; }

        public IVlcPlayerHandle Create(string url, Action<string> onError)
        {
            CapturedOnError = onError;
            return handle;
        }
    }

    private readonly TestVlcHandle _handle = new();
    private readonly TestVlcFactory _factory;
    private readonly RtspVideoPlayer _player;

    public RtspVideoPlayerTests()
    {
        _factory = new TestVlcFactory(_handle);
        _player = new RtspVideoPlayer("rtsps://host/stream", _factory);
    }

    [TearDown]
    public void TearDown() => _player.Dispose();

    private List<string> CaptureStatuses()
    {
        var statuses = new List<string>();
        _player.StatusChanged += (_, status) => statuses.Add(status);
        return statuses;
    }

    [Test]
    public void Start_CallsHandlePlay()
    {
        _player.Start();

        Assert.That(_handle.PlayCallCount, Is.EqualTo(1));
    }

    [Test]
    public void Start_RaisesStatusChangedConnecting()
    {
        var statuses = CaptureStatuses();

        _player.Start();

        Assert.That(statuses[^1], Is.EqualTo("Connecting..."));
    }

    [Test]
    public void Start_CalledTwice_DisposesFirstHandleBeforeCreatingNew()
    {
        var firstHandle = new TestVlcHandle();
        var secondHandle = new TestVlcHandle();
        var createCount = 0;
        var factory = new Mock<IVlcPlayerFactory>();
        factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<Action<string>>()))
               .Returns<string, Action<string>>((_, _) => createCount++ == 0 ? firstHandle : secondHandle);
        var player = new RtspVideoPlayer("rtsps://host", factory.Object);

        player.Start();
        player.Start();

        Assert.That(firstHandle.IsDisposed, Is.True);
        Assert.That(secondHandle.PlayCallCount, Is.EqualTo(1));
    }

    [Test]
    public void Stop_DisposesHandle()
    {
        _player.Start();

        _player.Stop();

        Assert.That(_handle.IsDisposed, Is.True);
    }

    [Test]
    public void Stop_WithoutStart_DoesNotThrow()
    {
        _player.Stop();
    }

    [Test]
    public void Dispose_CallsStop()
    {
        _player.Start();

        _player.Dispose();

        Assert.That(_handle.IsDisposed, Is.True);
    }

    [Test]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _player.Dispose();
        _player.Dispose();
    }

    [Test]
    public void FrameReady_OnHandle_IsRelayedToSubscribers()
    {
        _player.Start();

        VideoFrame? received = null;
        _player.FrameReady += (_, frame) => received = frame;

        var dataLength = 4 * 4 * 4;
        using var frame = new VideoFrame(ArrayPool<byte>.Shared.Rent(dataLength), 4, 4, dataLength);
        _handle.RaiseFrameReady(frame);

        Assert.That(received, Is.Not.Null);
        Assert.That(received.Width, Is.EqualTo(4));
        Assert.That(received.Height, Is.EqualTo(4));
    }

    [Test]
    public void StatusChanged_RaisedOnStart()
    {
        var statuses = CaptureStatuses();

        _player.Start();

        Assert.That(statuses, Does.Contain("Connecting..."));
    }

    [Test]
    public void StatusChanged_RelayedFromHandleErrorCallback()
    {
        var statuses = CaptureStatuses();

        _player.Start();
        _factory.CapturedOnError?.Invoke("TLS error");

        Assert.That(statuses[^1], Is.EqualTo("TLS error"));
    }

    [Test]
    public void Playing_RaisesConnectedStatusImmediately()
    {
        var statuses = CaptureStatuses();

        _player.Start();
        _handle.RaisePlaying();

        Assert.That(statuses[^1], Is.EqualTo("Connected"));
    }

    [Test]
    public void ScheduleReconnect_WhileStopped_DoesNothing()
    {
        _player.Start();
        _player.Stop();

        _player.ScheduleReconnect("test reason");

        Assert.That(_handle.PlayCallCount, Is.EqualTo(1));
    }

    [Test]
    public void ScheduleReconnect_RaisesStatusWithReason()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _player.ScheduleReconnect("Stream ended");

        Assert.That(statuses[^1], Is.EqualTo("Stream ended"));
    }

    [Test]
    public void ScheduleReconnect_CalledTwice_OnlyFirstTakesEffect()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _player.ScheduleReconnect("error 1");
        _player.ScheduleReconnect("error 2");

        Assert.That(statuses, Has.Count.EqualTo(1));
        Assert.That(statuses[0], Is.EqualTo("error 1"));
    }

    [Test]
    public void EncounteredError_OnHandle_TriggersReconnect()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _handle.RaiseEncounteredError();

        Assert.That(statuses[^1], Is.EqualTo("Playback error"));
    }

    [Test]
    public void EndReached_OnHandle_TriggersReconnect()
    {
        _player.Start();
        var statuses = CaptureStatuses();

        _handle.RaiseEndReached();

        Assert.That(statuses[^1], Is.EqualTo("Stream ended"));
    }

    [Test]
    public void TearDownPlayer_DisposesHandle()
    {
        _player.Start();

        _player.TearDownPlayer();

        Assert.That(_handle.IsDisposed, Is.True);
    }

    [Test]
    public void TearDownPlayer_WhenNoHandleExists_DoesNotThrow()
    {
        _player.TearDownPlayer();
    }

    [Test]
    public void StartInternal_WhenStopped_DoesNotCreateHandle()
    {
        var factory = new Mock<IVlcPlayerFactory>();
        var player = new RtspVideoPlayer("rtsps://host", factory.Object);
        player.Stop();

        player.StartInternal();

        factory.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Never());
    }
}
