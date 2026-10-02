using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.Domain.Events;
using Surveil.Services;
using Surveil.Services.Interfaces;
using Surveil.Views;

namespace Surveil.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly ICameraProvider _apiClient;
    private readonly ICameraEventStream _eventStream;
    private readonly IDesktopNotifier _notifier;
    private readonly ISnapshotGrabber _snapshotGrabber;
    private readonly ICameraStreamCatalog _streamCatalog;
    private readonly ISettingsChangeNotifier _settingsNotifier;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly CancellationTokenSource _cts = new();

    public ObservableCollection<Camera> Cameras { get; } = [];

    public Camera? SelectedCamera
    {
        get;
        set => SetProperty(ref field, value);
    }

    public MainViewModel(
        MainWindow mainWindow,
        ICameraProvider apiClient,
        ICameraEventStream eventStream,
        IDesktopNotifier notifier,
        ISnapshotGrabber snapshotGrabber,
        ICameraStreamCatalog streamCatalog,
        ISettingsChangeNotifier settingsNotifier,
        DispatcherQueue dispatcherQueue)
    {
        _mainWindow = mainWindow;
        _apiClient = apiClient;
        _eventStream = eventStream;
        _notifier = notifier;
        _snapshotGrabber = snapshotGrabber;
        _streamCatalog = streamCatalog;
        _settingsNotifier = settingsNotifier;
        _dispatcherQueue = dispatcherQueue;

        _settingsNotifier.SettingsChanged += OnSettingsChanged;

        _ = InitializeCamerasAsync(_cts.Token);
        _ = SubscribeToEventsAsync(_cts.Token);
    }

    private void OnSettingsChanged(AppSettings settings)
    {
        _dispatcherQueue.TryEnqueue(Cameras.Clear);
        _ = InitializeCamerasAsync(_cts.Token);
    }

    private async Task InitializeCamerasAsync(CancellationToken ct)
    {
        try
        {
            var cameras = await _apiClient.GetCamerasAsync(ct);
            _streamCatalog.Prefetch(cameras.Select(c => c.Id));
            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var camera in cameras)
                    Cameras.Add(camera);

                SelectedCamera = cameras.Count > 0 ? cameras[0] : null;
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] Camera discovery failed: {ex.Message}");
        }
    }

    private async Task SubscribeToEventsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var @event in _eventStream.SubscribeAsync(ct))
            {
                var camera = Cameras.FirstOrDefault(c => c.Id == @event.DeviceId) ?? SelectedCamera;
                var needsSnapshot = camera is not null && !_mainWindow.IsStreaming(camera.Id);
                _ = NotifyAsync(@event, camera, needsSnapshot, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] Event stream error: {ex.Message}");
        }
    }

    private async Task NotifyAsync(CameraEvent cameraEvent, Camera? camera, bool needsSnapshot, CancellationToken ct)
    {
        try
        {
            if (needsSnapshot && camera is not null)
                await _snapshotGrabber.GrabAsync(camera.Id, ct);

            _notifier.Notify(cameraEvent, camera?.Name ?? "Unknown Camera");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] Notification failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void LeftClick() => _mainWindow.ShowFromBackground();

    [RelayCommand]
    private void Exit() => _mainWindow.ExitApplication();

    public void Dispose()
    {
        _settingsNotifier.SettingsChanged -= OnSettingsChanged;
        _cts.Cancel();
        _cts.Dispose();
    }
}
