using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Application.Ports;
using Surveil.Application.Settings;
using Surveil.Domain.Cameras;
using Surveil.Infrastructure.Http;
using Surveil.Infrastructure.Settings;

namespace Surveil.Core.Tests.Infrastructure;

[TestClass]
public sealed class ReloadableCameraProviderTests
{
    private sealed class StubCameraProvider : ICameraProvider
    {
        public Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Camera>>([new Camera("cam1", "Stub", true)]);

        public Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RtspsStream>>([]);

        public Task<IReadOnlyList<RtspsStream>> CreateRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RtspsStream>>([new RtspsStream("rtsp://stub", "high")]);
    }

    private sealed class StubFactory(VideoProviderType type, bool canCreate = true) : ICameraProviderFactory
    {
        public VideoProviderType ProviderType => type;

        public ICameraProvider? Create(AppSettings settings) =>
            canCreate ? new StubCameraProvider() : null;
    }

    private readonly SettingsChangeNotifier _notifier = new();

    private ReloadableCameraProvider CreateProvider(
        AppSettings settings,
        params ICameraProviderFactory[] factories) =>
        new(settings, _notifier, factories);

    private static AppSettings NoneSettings() => new() { SelectedProvider = VideoProviderType.None };

    private static AppSettings UnifiSettings() => new()
    {
        SelectedProvider = VideoProviderType.UnifiProtect,
        UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://host", ApiKey = "key" }
    };

    [TestMethod]
    public async Task InitialSettingsNone_DelegatesToNoOpProvider()
    {
        var provider = CreateProvider(NoneSettings(), new StubFactory(VideoProviderType.UnifiProtect));

        var cameras = await provider.GetCamerasAsync();

        Assert.IsEmpty(cameras);
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.CreateRtspsStreamsAsync("cam1"));
    }

    [TestMethod]
    public async Task SettingsChanged_ToNone_SwapsToNoOpProvider()
    {
        var provider = CreateProvider(UnifiSettings(), new StubFactory(VideoProviderType.UnifiProtect));

        _notifier.NotifyChanged(NoneSettings());

        var cameras = await provider.GetCamerasAsync();
        Assert.IsEmpty(cameras);
    }

    [TestMethod]
    public async Task MatchingFactory_IsUsedForTheSelectedProvider()
    {
        var provider = CreateProvider(UnifiSettings(), new StubFactory(VideoProviderType.UnifiProtect));

        var cameras = await provider.GetCamerasAsync();

        Assert.HasCount(1, cameras);
        Assert.AreEqual("Stub", cameras[0].Name);
    }

    [TestMethod]
    public async Task FactoryReturningNull_FallsBackToNoOpProvider()
    {
        var provider = CreateProvider(
            UnifiSettings(), new StubFactory(VideoProviderType.UnifiProtect, canCreate: false));

        var cameras = await provider.GetCamerasAsync();

        Assert.IsEmpty(cameras);
    }

    [TestMethod]
    public async Task NoFactoryForSelectedProvider_FallsBackToNoOpProvider()
    {
        var provider = CreateProvider(UnifiSettings());

        var cameras = await provider.GetCamerasAsync();

        Assert.IsEmpty(cameras);
    }
}
