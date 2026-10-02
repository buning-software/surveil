using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Surveil.Services;

public sealed class ProgressiveVideoPlayer : IDisposable
{
    private readonly Lock _lock = new();
    private readonly RtspVideoPlayer _main;
    private readonly RtspVideoPlayer? _preview;
    private RtspVideoPlayer _active;
    private int _generation;

    public event EventHandler<VideoFrame>? FrameReady;
    public event EventHandler<string>? StatusChanged;

    public ProgressiveVideoPlayer(IReadOnlyList<string> urlsBestFirst)
        : this(urlsBestFirst, new DefaultVlcPlayerFactory()) { }

    internal ProgressiveVideoPlayer(IReadOnlyList<string> urlsBestFirst, IVlcPlayerFactory factory)
    {
        if (urlsBestFirst.Count == 0)
            throw new ArgumentException("At least one stream URL is required.", nameof(urlsBestFirst));

        _main = new RtspVideoPlayer(urlsBestFirst[0], factory);
        _main.FrameReady += OnMainFrame;
        _main.StatusChanged += OnStatusChanged;

        if (urlsBestFirst.Count > 1 && urlsBestFirst[^1] != urlsBestFirst[0])
        {
            _preview = new RtspVideoPlayer(urlsBestFirst[^1], factory);
            _preview.FrameReady += OnPreviewFrame;
            _preview.StatusChanged += OnStatusChanged;
        }

        _active = _preview ?? _main;
    }

    public void Start()
    {
        lock (_lock)
        {
            _generation++;
            _active = _preview ?? _main;
        }

        _preview?.Start();
        _main.Start();
    }

    public void Stop()
    {
        lock (_lock)
            _generation++;

        _preview?.Stop();
        _main.Stop();
    }

    public void Dispose()
    {
        lock (_lock)
            _generation++;

        _preview?.Dispose();
        _main.Dispose();
    }

    private void OnMainFrame(object? sender, VideoFrame frame)
    {
        int? retireGeneration = null;

        lock (_lock)
        {
            if (_active != _main)
            {
                _active = _main;
                retireGeneration = _generation;
            }
        }

        if (retireGeneration is { } generation && _preview is { } preview)
            _ = Task.Run(() => RetirePreview(preview, generation));

        FrameReady?.Invoke(this, frame);
    }

    private void OnPreviewFrame(object? sender, VideoFrame frame)
    {
        lock (_lock)
        {
            if (_active != _preview)
            {
                frame.Dispose();
                return;
            }
        }

        FrameReady?.Invoke(this, frame);
    }

    private void RetirePreview(RtspVideoPlayer preview, int generation)
    {
        lock (_lock)
        {
            if (generation != _generation) return;
        }

        preview.Stop();
    }

    private void OnStatusChanged(object? sender, string message)
    {
        lock (_lock)
        {
            if (sender != _active) return;
        }

        StatusChanged?.Invoke(this, message);
    }
}
