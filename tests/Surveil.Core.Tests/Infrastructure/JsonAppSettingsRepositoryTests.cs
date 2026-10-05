using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Surveil.Application.Settings;
using Surveil.Infrastructure.Settings;

namespace Surveil.Core.Tests.Infrastructure;

[TestFixture]
public sealed class JsonAppSettingsRepositoryTests
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"settings-tests-{Guid.NewGuid():N}");

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void Constructor_CreatesDirectory()
    {
        _ = new JsonAppSettingsRepository(_tempDir);

        Assert.That(Directory.Exists(_tempDir), Is.True);
    }

    [Test]
    public async Task LoadAsync_NoSettingsFile_ReturnsDefaults()
    {
        var repository = new JsonAppSettingsRepository(_tempDir);

        var settings = await repository.LoadAsync();

        Assert.That(settings, Is.EqualTo(new AppSettings()));
    }

    [Test]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettings()
    {
        var saved = new AppSettings
        {
            SelectedProvider = VideoProviderType.UnifiProtect,
            UnifiProtect = new UnifiProtectProviderSettings { BaseUrl = "https://nvr", ApiKey = "key", SnapshotPath = @"C:\snap.jpg" }
        };
        await new JsonAppSettingsRepository(_tempDir).SaveAsync(saved);

        var loaded = await new JsonAppSettingsRepository(_tempDir).LoadAsync();

        Assert.That(loaded, Is.EqualTo(saved));
    }

    [Test]
    public async Task SaveAsync_WritesIntoGivenDirectory()
    {
        await new JsonAppSettingsRepository(_tempDir).SaveAsync(new AppSettings());

        Assert.That(File.Exists(Path.Combine(_tempDir, "settings.json")), Is.True);
    }

    [Test]
    public async Task LoadAsync_CorruptFile_ReturnsDefaults()
    {
        var repository = new JsonAppSettingsRepository(_tempDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "settings.json"), "{ not json");

        var settings = await repository.LoadAsync();

        Assert.That(settings, Is.EqualTo(new AppSettings()));
    }
}
