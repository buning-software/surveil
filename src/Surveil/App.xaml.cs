using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Infrastructure.Http;
using Surveil.Infrastructure.Settings;
using Surveil.Infrastructure.Startup;
using Surveil.Services;
using Surveil.Services.Interfaces;
using Surveil.Unifi;
using Surveil.ViewModels;
using Surveil.Views;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Surveil;

public sealed partial class App
{
    private static string DefaultSnapshotPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Surveil", "snapshots", "snapshot.jpg");

    private MainWindow? _mainWindow;
    private bool _isShowingErrorDialog;

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            UnhandledException += OnUnhandledException;

            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            var keyInstance = AppInstance.FindOrRegisterForKey("Surveil");

            if (!keyInstance.IsCurrent)
            {
                await keyInstance.RedirectActivationToAsync(activationArgs);
                Exit();
                return;
            }

            var services = new ServiceCollection();
            await AddAppServicesAsync(services);
            var provider = services.BuildServiceProvider();

            Ioc.Default.ConfigureServices(provider);

            _ = Task.Run(RtspVideoPlayer.WarmUp);

            _mainWindow = provider.GetRequiredService<MainWindow>();

            keyInstance.Activated += OnActivated;

            if (activationArgs.Kind != ExtendedActivationKind.StartupTask)
                _mainWindow.BringToFront();

            AppNotificationManager.Default.NotificationInvoked += (_, _) => _mainWindow.ShowFromBackground();
            AppNotificationManager.Default.Register();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"App startup failed: {ex.Message}");
        }
    }

    private static async Task AddAppServicesAsync(IServiceCollection services)
    {
        var settingsRepository = new JsonAppSettingsRepository();
        var appSettings = await settingsRepository.LoadAsync();

        services.AddSingleton(new SnapshotOptions(appSettings.UnifiProtect.SnapshotPath ?? DefaultSnapshotPath));
        services.AddSingleton(Options.Create(new EventNotificationSettings()));
        services.AddSingleton(Options.Create(new UnifiProtectOptions
        {
            BaseUrl = appSettings.UnifiProtect.BaseUrl,
            ApiKey = appSettings.UnifiProtect.ApiKey
        }));

        services.AddSingleton<IAppSettingsRepository>(settingsRepository);
        services.AddSingleton(appSettings);
        services.AddSingleton<ISettingsChangeNotifier, SettingsChangeNotifier>();
        services.AddSingleton<IStartupTaskService, WindowsStartupTaskService>();

        services.AddSingleton<ICameraProviderFactory, UnifiCameraProviderFactory>();
        services.AddSingleton<ICameraProvider, ReloadableCameraProvider>();
        services.AddSingleton<ICameraEventStream, ProtectEventStream>();
        services.AddTransient<IDesktopNotifier, DesktopNotifier>();
        services.AddSingleton<ICameraStreamCatalog, CameraStreamCatalog>();
        services.AddSingleton<ISnapshotGrabber, SnapshotGrabber>();
        services.AddTransient<SettingsViewModel>();

        services.AddSingleton<MainWindow>();
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        if (args.Kind == ExtendedActivationKind.StartupTask) return;

        _mainWindow?.ShowFromBackground();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");

        if (_mainWindow is null || _isShowingErrorDialog) return;

        _isShowingErrorDialog = true;
        _mainWindow.DispatcherQueue.TryEnqueue(async () => await ShowErrorDialogAsync());
    }

    private async Task ShowErrorDialogAsync()
    {
        try
        {
            if (_mainWindow?.Content?.XamlRoot is null) return;

            var dialog = new ContentDialog
            {
                Title = "Something went wrong",
                Content = "Surveil ran into an unexpected error and had to recover. If this keeps happening, please report it.",
                CloseButtonText = "OK",
                XamlRoot = _mainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to show error dialog: {ex.Message}");
        }
        finally
        {
            _isShowingErrorDialog = false;
        }
    }
}
