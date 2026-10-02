using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;

namespace Surveil.Infrastructure.Http;

public sealed class ReloadableCameraProvider : ICameraProvider
{
    private readonly Lock _lock = new();
    private readonly IReadOnlyList<ICameraProviderFactory> _factories;
    private ICameraProvider _current;
    private AppSettings _currentSettings;

    public ReloadableCameraProvider(
        AppSettings initialSettings,
        ISettingsChangeNotifier notifier,
        IEnumerable<ICameraProviderFactory> factories)
    {
        _factories = factories.ToList();
        _currentSettings = initialSettings;
        _current = Build(initialSettings);

        notifier.SettingsChanged += settings =>
        {
            ICameraProvider? previous = null;

            lock (_lock)
            {
                if (settings == _currentSettings) return;

                previous = _current;
                _currentSettings = settings;
                _current = Build(settings);
            }

            (previous as IDisposable)?.Dispose();
        };
    }

    public Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default) =>
        Current.GetCamerasAsync(ct);

    public Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        Current.GetRtspsStreamsAsync(cameraId, ct);

    public Task<IReadOnlyList<RtspsStream>> CreateRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        Current.CreateRtspsStreamsAsync(cameraId, ct);

    private ICameraProvider Current
    {
        get { lock (_lock) return _current; }
    }

    private ICameraProvider Build(AppSettings settings)
    {
        var factory = _factories.FirstOrDefault(f => f.ProviderType == settings.SelectedProvider);
        return factory?.Create(settings) ?? new NoOpCameraProvider();
    }
}
