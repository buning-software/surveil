using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Surveil.Application.Ports;
using Surveil.Domain.Cameras;
using Surveil.Services;
using Surveil.Services.Interfaces;
using Surveil.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace Surveil.Views;

public sealed partial class MainWindow
{
    private const int WindowWidth = 1350;
    private const int WindowHeight = 800;

    private bool _isExiting;

    public MainViewModel ViewModel { get; }

    public MainWindow(
        ICameraProvider apiClient,
        ICameraEventStream eventStream,
        IDesktopNotifier notifier,
        ISnapshotGrabber snapshotGrabber,
        ICameraStreamCatalog streamCatalog,
        ISettingsChangeNotifier settingsNotifier)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ResizeAndCenter();

        ViewModel = new MainViewModel(
            this,
            apiClient,
            eventStream,
            notifier,
            snapshotGrabber,
            streamCatalog,
            settingsNotifier,
            DispatcherQueue.GetForCurrentThread());
        RootGrid.DataContext = ViewModel;

        ViewModel.Cameras.CollectionChanged += OnCamerasChanged;

        Closed += OnWindowClosed;
        AppWindow.Changed += OnAppWindowChanged;
        ExitMenuItem.Command = ViewModel.ExitCommand;

        TaskBarIcon.ForceCreate();
    }

    public void BringToFront()
    {
        var handle = WindowNative.GetWindowHandle(this);

        this.Show();
        this.ShowInTaskbar();

        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();

        Activate();
        SetForegroundWindow(handle);

        UpdateStreamState();
    }

    public bool IsStreaming(string cameraId) =>
        CurrentCameraViewModel?.IsStreaming(cameraId) ?? false;

    public void ShowFromBackground() => DispatcherQueue.TryEnqueue(BringToFront);

    public void ExitApplication()
    {
        _isExiting = true;
        this.Hide();
        ViewModel.Dispose();
        TaskBarIcon.Dispose();
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private void ResizeAndCenter()
    {
        var workArea = DisplayArea.Primary.WorkArea;
        var width = Math.Min(WindowWidth, workArea.Width);
        var height = Math.Min(WindowHeight, workArea.Height);
        var x = workArea.X + (workArea.Width - width) / 2;
        var y = workArea.Y + (workArea.Height - height) / 2;

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
    }

    private void OnCamerasChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            CamerasGroup.MenuItems.Clear();
            return;
        }

        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null) return;

        foreach (Camera camera in e.NewItems)
        {
            var item = new NavigationViewItem
            {
                Content = camera.Name,
                Tag = camera
            };
            ToolTipService.SetToolTip(item, camera.Name);
            CamerasGroup.MenuItems.Add(item);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage), null, new SuppressNavigationTransitionInfo());
            return;
        }

        if (args.SelectedItem is not NavigationViewItem { Tag: Camera camera }) return;

        ViewModel.SelectedCamera = camera;
        ContentFrame.Navigate(typeof(CameraView), camera, new SuppressNavigationTransitionInfo());
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_isExiting) return;

        args.Handled = true;
        this.Hide();

        UpdateStreamState();
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidVisibilityChange || args.DidPresenterChange)
            UpdateStreamState();
    }

    private void UpdateStreamState()
    {
        if (CurrentCameraViewModel is not { } viewModel) return;

        var minimized = AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized };
        if (AppWindow.IsVisible && !minimized)
            viewModel.Resume();
        else
            viewModel.Pause();
    }

    private CameraViewModel? CurrentCameraViewModel => (ContentFrame.Content as CameraView)?.ViewModel;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);
}
