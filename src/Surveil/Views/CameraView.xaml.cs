using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.Services;
using Surveil.ViewModels;

namespace Surveil.Views;

public sealed partial class CameraView : Page
{
    internal CameraViewModel? ViewModel { get; private set; }

    public CameraView()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Camera camera) return;

        var streamCatalog = Ioc.Default.GetRequiredService<ICameraStreamCatalog>();
        var snapshot = Ioc.Default.GetRequiredService<SnapshotOptions>();

        ViewModel = new CameraViewModel(camera, streamCatalog, snapshot, DispatcherQueue);
        DataContext = ViewModel;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        ViewModel?.Dispose();
        ViewModel = null;
    }
}
