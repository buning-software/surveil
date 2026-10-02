using System;
using System.Threading;
using System.Threading.Tasks;

namespace Surveil.Services;

public sealed class RtspVideoPlayer : IDisposable
{
    private const int ReconnectIdle = 0;
    private const int ReconnectScheduled = 1;

    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromMilliseconds(500);

    private readonly string _url;
    private readonly IVlcPlayerFactory _factory;
    private readonly Lock _handleLock = new();
    private IVlcPlayerHandle? _handle;
    private CancellationTokenSource _cts = new();
    private bool _stopped;
    private bool _disposed;
    private int _reconnectPending = ReconnectIdle;

    public event EventHandler<VideoFrame>? FrameReady;
    public event EventHandler<string>? StatusChanged;

    public static void WarmUp() => DefaultVlcPlayerFactory.WarmUp();

    public RtspVideoPlayer(string url)
        : this(url, new DefaultVlcPlayerFactory()) { }

    internal RtspVideoPlayer(string url, IVlcPlayerFactory factory)
    {
        _url = url;
        _factory = factory;
    }

    public void Start()
    {
        lock (_handleLock)
        {
            if (_disposed) return;

            TearDownPlayerLocked();
            _stopped = false;
        }

        Interlocked.Exchange(ref _reconnectPending, ReconnectIdle);
        _cts = new CancellationTokenSource();
        StartInternal();
    }

    public void Stop()
    {
        _stopped = true;
        _cts.Cancel();
        TearDownPlayer();
    }

    public void Dispose()
    {
        lock (_handleLock)
        {
            _disposed = true;
        }

        Stop();
    }

    internal void StartInternal()
    {
        lock (_handleLock)
        {
            if (_stopped || _disposed) return;

            StatusChanged?.Invoke(this, "Connecting...");

            var handle = _factory.Create(_url, msg => StatusChanged?.Invoke(this, msg));
            handle.Playing += OnHandlePlaying;
            handle.EncounteredError += (_, _) => ScheduleReconnect("Playback error");
            handle.EndReached += (_, _) => ScheduleReconnect("Stream ended");
            handle.FrameReady += (_, frame) => FrameReady?.Invoke(this, frame);

            _handle = handle;
            handle.Play();
        }
    }

    internal void ScheduleReconnect(string reason)
    {
        if (_stopped) return;
        if (Interlocked.CompareExchange(ref _reconnectPending, ReconnectScheduled, ReconnectIdle) != ReconnectIdle)
            return;

        StatusChanged?.Invoke(this, reason);

        var token = _cts.Token;
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ReconnectDelay, token);
                TearDownPlayer();
                Interlocked.Exchange(ref _reconnectPending, ReconnectIdle);
                StartInternal();
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _reconnectPending, ReconnectIdle);
            }
        }, token);
    }

    internal void TearDownPlayer()
    {
        lock (_handleLock)
        {
            TearDownPlayerLocked();
        }
    }

    private void OnHandlePlaying(object? sender, EventArgs e)
    {
        StatusChanged?.Invoke(this, "Connected");
    }

    private void TearDownPlayerLocked()
    {
        _handle?.Dispose();
        _handle = null;
    }
}
