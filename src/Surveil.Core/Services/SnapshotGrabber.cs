using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Settings;

namespace Surveil.Services;

public interface ISnapshotGrabber
{
    Task<bool> GrabAsync(string cameraId, CancellationToken ct = default);
}

public sealed class SnapshotGrabber : ISnapshotGrabber, IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);

    private readonly ICameraStreamCatalog _streamCatalog;
    private readonly IVlcPlayerFactory _factory;
    private readonly Func<int, int, byte[], Task> _save;
    private readonly TimeSpan _timeout;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SnapshotGrabber(ICameraStreamCatalog streamCatalog, SnapshotOptions snapshot)
        : this(streamCatalog, new DefaultVlcPlayerFactory(), new SnapshotService(snapshot.Path).SaveNowAsync, DefaultTimeout) { }

    internal SnapshotGrabber(
        ICameraStreamCatalog streamCatalog,
        IVlcPlayerFactory factory,
        Func<int, int, byte[], Task> save,
        TimeSpan timeout)
    {
        _streamCatalog = streamCatalog;
        _factory = factory;
        _save = save;
        _timeout = timeout;
    }

    public async Task<bool> GrabAsync(string cameraId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var streams = await _streamCatalog.GetStreamsAsync(cameraId, ct);
            var frame = await CaptureFirstFrameAsync(streams[^1].Url, ct);
            if (frame is not var (width, height, pixels)) return false;

            await _save(width, height, pixels);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SnapshotGrabber] Grab failed: {ex.Message}");
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task<(int Width, int Height, byte[] Pixels)?> CaptureFirstFrameAsync(string url, CancellationToken ct)
    {
        var captured = new TaskCompletionSource<(int Width, int Height, byte[] Pixels)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var player = new RtspVideoPlayer(url, _factory);
        player.FrameReady += (_, frame) =>
        {
            using (frame)
            {
                if (captured.Task.IsCompleted) return;
                captured.TrySetResult((frame.Width, frame.Height, frame.Pixels.AsSpan(0, frame.DataLength).ToArray()));
            }
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            await Task.Run(player.Start, timeoutCts.Token);
            return await captured.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }
}
