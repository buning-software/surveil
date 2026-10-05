using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
public sealed class ProgressiveVideoPlayerTests
{
    private const string HighUrl = "rtsp://host/high";
    private const string LowUrl = "rtsp://host/low";

    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private sealed class TestVlcHandle(string url) : IVlcPlayerHandle
    {
        public event EventHandler? Playing;
        public event EventHandler? EncounteredError;
        public event EventHandler? EndReached;
        public event EventHandler<VideoFrame>? FrameReady;

        public string Url { get; } = url;
        public bool IsDisposed { get; private set; }

        public void Play() { }
        public void Stop() { }
        public void Dispose() => IsDisposed = true;

        public void RaisePlaying() => Playing?.Invoke(this, EventArgs.Empty);
        public void RaiseError() => EncounteredError?.Invoke(this, EventArgs.Empty);
        public void RaiseEnd() => EndReached?.Invoke(this, EventArgs.Empty);
        public VideoFrame RaiseFrame(int width)
        {
            var frame = CreateFrame(width, 4);
            FrameReady?.Invoke(this, frame);
            return frame;
        }
    }

    private sealed class TestVlcFactory : IVlcPlayerFactory
    {
        private readonly List<TestVlcHandle> _handles = [];

        public IReadOnlyList<TestVlcHandle> Handles
        {
            get { lock (_handles) return _handles.ToList(); }
        }

        public TestVlcHandle Latest(string url) => Handles.Last(h => h.Url == url);

        public IVlcPlayerHandle Create(string url, Action<string> onError)
        {
            var handle = new TestVlcHandle(url);
            lock (_handles) _handles.Add(handle);
            return handle;
        }
    }

    private readonly TestVlcFactory _factory = new();
    private readonly List<VideoFrame> _forwarded = [];
    private readonly List<string> _statuses = [];

    private static VideoFrame CreateFrame(int width, int height)
    {
        var dataLength = width * height * 4;
        return new VideoFrame(ArrayPool<byte>.Shared.Rent(dataLength), width, height, dataLength);
    }

    private ProgressiveVideoPlayer CreatePlayer(params string[] urlsBestFirst)
    {
        var player = new ProgressiveVideoPlayer(urlsBestFirst, _factory);
        player.FrameReady += (_, frame) => _forwarded.Add(frame);
        player.StatusChanged += (_, status) => _statuses.Add(status);
        return player;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + WaitTimeout;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10);
    }

    [Test]
    public void Constructor_NoUrls_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ProgressiveVideoPlayer([], _factory));
    }

    [Test]
    public void Start_TwoQualities_OpensBothStreams()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);

        player.Start();

        Assert.That(_factory.Handles.Select(h => h.Url).ToArray(), Is.EquivalentTo(new[] { HighUrl, LowUrl }));
    }

    [Test]
    public void Start_SingleQuality_OpensOnlyThatStream()
    {
        using var player = CreatePlayer(HighUrl);

        player.Start();

        Assert.That(_factory.Handles, Has.Exactly(1).Items);
        Assert.That(_factory.Handles[0].Url, Is.EqualTo(HighUrl));
    }

    [Test]
    public void Start_SameUrlForBothEnds_OpensOneStream()
    {
        using var player = CreatePlayer(HighUrl, HighUrl);

        player.Start();

        Assert.That(_factory.Handles, Has.Exactly(1).Items);
    }

    [Test]
    public void PreviewFrame_BeforeTheBestStreamDelivers_IsForwarded()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();

        var frame = _factory.Latest(LowUrl).RaiseFrame(width: 2);

        Assert.That(_forwarded, Has.Exactly(1).Items);
        Assert.That(_forwarded[0], Is.SameAs(frame));
    }

    [Test]
    public void BestStreamFrame_IsForwarded()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();

        var frame = _factory.Latest(HighUrl).RaiseFrame(width: 8);

        Assert.That(_forwarded, Has.Exactly(1).Items);
        Assert.That(_forwarded[0], Is.SameAs(frame));
    }

    [Test]
    public void PreviewFrame_AfterTheBestStreamDelivered_IsDropped()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();
        var preview = _factory.Latest(LowUrl);
        _factory.Latest(HighUrl).RaiseFrame(width: 8);

        preview.RaiseFrame(width: 2);

        Assert.That(_forwarded, Has.Exactly(1).Items);
        Assert.That(_forwarded[0].Width, Is.EqualTo(8));
    }

    [Test]
    public async Task BestStreamFirstFrame_StopsThePreviewStream()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();
        var preview = _factory.Latest(LowUrl);

        _factory.Latest(HighUrl).RaiseFrame(width: 8);
        await WaitUntilAsync(() => preview.IsDisposed);

        Assert.That(preview.IsDisposed, Is.True);
        Assert.That(_factory.Latest(HighUrl).IsDisposed, Is.False);
    }

    [Test]
    public void Status_FromTheBestStreamBeforeItTakesOver_IsSuppressed()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();
        _statuses.Clear();

        _factory.Latest(HighUrl).RaisePlaying();

        Assert.That(_statuses, Is.Empty);
    }

    [Test]
    public void Status_FromThePreviewStream_IsForwarded()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();
        _statuses.Clear();

        _factory.Latest(LowUrl).RaisePlaying();

        Assert.That(_statuses.Single(), Is.EqualTo("Connected"));
    }

    [Test]
    public void Status_FromTheBestStreamAfterItTookOver_IsForwarded()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();
        _factory.Latest(HighUrl).RaiseFrame(width: 8);
        _statuses.Clear();

        _factory.Latest(HighUrl).RaisePlaying();

        Assert.That(_statuses.Single(), Is.EqualTo("Connected"));
    }

    [Test]
    public void Stop_StopsBothStreams()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();

        player.Stop();

        Assert.That(_factory.Handles.All(h => h.IsDisposed), Is.True);
    }

    [Test]
    public void Dispose_DisposesBothStreams()
    {
        var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();

        player.Dispose();

        Assert.That(_factory.Handles.All(h => h.IsDisposed), Is.True);
    }

    [Test]
    public void Start_AfterTheBestStreamTookOver_ShowsThePreviewAgain()
    {
        using var player = CreatePlayer(HighUrl, LowUrl);
        player.Start();
        _factory.Latest(HighUrl).RaiseFrame(width: 8);
        player.Stop();
        _forwarded.Clear();

        player.Start();
        _factory.Latest(LowUrl).RaiseFrame(width: 2);

        Assert.That(_forwarded, Has.Exactly(1).Items);
        Assert.That(_forwarded[0].Width, Is.EqualTo(2));
    }
}
