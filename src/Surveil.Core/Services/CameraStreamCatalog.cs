using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;

namespace Surveil.Services;

public interface ICameraStreamCatalog
{
    Task<IReadOnlyList<RtspsStream>> GetStreamsAsync(string cameraId, CancellationToken ct = default);
    void Prefetch(IEnumerable<string> cameraIds);
}

public sealed class CameraStreamCatalog : ICameraStreamCatalog, IDisposable
{
    private readonly ICameraProvider _provider;
    private readonly ISettingsChangeNotifier _settingsNotifier;
    private readonly Lock _lock = new();
    private Dictionary<string, Task<IReadOnlyList<RtspsStream>>> _streams = [];
    private CancellationTokenSource _cts = new();

    public CameraStreamCatalog(ICameraProvider provider, ISettingsChangeNotifier settingsNotifier)
    {
        _provider = provider;
        _settingsNotifier = settingsNotifier;
        _settingsNotifier.SettingsChanged += OnSettingsChanged;
    }

    public Task<IReadOnlyList<RtspsStream>> GetStreamsAsync(string cameraId, CancellationToken ct = default) =>
        GetOrStart(cameraId).WaitAsync(ct);

    public void Prefetch(IEnumerable<string> cameraIds)
    {
        foreach (var cameraId in cameraIds)
            _ = GetOrStart(cameraId).ContinueWith(
                t => Debug.WriteLine($"[CameraStreamCatalog] Prefetch failed for {cameraId}: {t.Exception?.GetBaseException().Message}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
    }

    public void Dispose()
    {
        _settingsNotifier.SettingsChanged -= OnSettingsChanged;
        lock (_lock)
            _cts.Cancel();
    }

    private Task<IReadOnlyList<RtspsStream>> GetOrStart(string cameraId)
    {
        lock (_lock)
        {
            if (_streams.TryGetValue(cameraId, out var cached) && !cached.IsFaulted && !cached.IsCanceled)
                return cached;

            var task = LoadAsync(cameraId, _cts.Token);
            _streams[cameraId] = task;
            return task;
        }
    }

    private async Task<IReadOnlyList<RtspsStream>> LoadAsync(string cameraId, CancellationToken ct)
    {
        var streams = await _provider.GetRtspsStreamsAsync(cameraId, ct);
        if (streams.Count > 1) return streams;

        try
        {
            var created = await _provider.CreateRtspsStreamsAsync(cameraId, ct);
            if (created.Count > 0) return created;
        }
        catch (Exception ex) when (streams.Count > 0 && ex is not OperationCanceledException)
        {
            Debug.WriteLine($"[CameraStreamCatalog] Could not add streams for {cameraId}, using the existing one: {ex.Message}");
        }

        return streams.Count > 0
            ? streams
            : throw new InvalidOperationException($"No RTSPS stream is available for camera {cameraId}.");
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        lock (_lock)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _streams = [];
        }
    }
}
