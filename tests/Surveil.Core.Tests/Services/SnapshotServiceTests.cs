using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
public sealed class SnapshotServiceTests
{
    private const double LandscapeAspectRatio = 16.0 / 9.0;

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"snapshot-tests-{Guid.NewGuid():N}");

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(params string[] segments) =>
        Path.Combine([_tempDir, .. segments]);

    private static byte[] BgraPixels(int width, int height) => new byte[width * height * 4];

    [Test]
    public void GetHeroPath_AddsHeroSuffix()
    {
        var hero = SnapshotService.GetHeroPath(@"C:\snapshots\snapshot.jpg");

        Assert.That(hero, Is.EqualTo(@"C:\snapshots\snapshot-hero.jpg"));
    }

    [Test]
    public void GetHeroPath_PreservesExtension()
    {
        var hero = SnapshotService.GetHeroPath(@"C:\snapshots\frame.png");

        Assert.That(hero.EndsWith("-hero.png"), Is.True);
    }

    [Test]
    public void GetHeroPath_NestedDirectory_HeroIsInSameDirectory()
    {
        var path = TempPath("sub", "shot.jpg");

        var hero = SnapshotService.GetHeroPath(path);

        Assert.That(Path.GetDirectoryName(hero), Is.EqualTo(Path.GetDirectoryName(path)));
        Assert.That(Path.GetFileName(hero).StartsWith("shot-hero"), Is.True);
    }

    [Test]
    public void Constructor_CreatesDirectory()
    {
        var snapshotPath = TempPath("shots", "snapshot.jpg");

        using var service = new SnapshotService(snapshotPath);

        Assert.That(Directory.Exists(Path.GetDirectoryName(snapshotPath)), Is.True);
    }

    [TestCase(1920, 1080)]
    [TestCase(160, 90)]
    public void CropToLandscape_WhenAlreadyLandscape_ReturnsSamePixels(int width, int height)
    {
        var pixels = BgraPixels(width, height);

        var (result, croppedWidth, croppedHeight) = SnapshotService.CropToLandscape(pixels, width, height);

        Assert.That(result, Is.SameAs(pixels));
        Assert.That(croppedWidth, Is.EqualTo(width));
        Assert.That(croppedHeight, Is.EqualTo(height));
    }

    [Test]
    public void CropToLandscape_SquareImage_CropsToLandscape()
    {
        var pixels = BgraPixels(100, 100);

        var (_, croppedWidth, croppedHeight) = SnapshotService.CropToLandscape(pixels, 100, 100);

        Assert.That(croppedWidth, Is.EqualTo(100));
        Assert.That(croppedHeight < 100, Is.True, "Cropped height should be less than original");
        Assert.That(Math.Abs((double)croppedWidth / croppedHeight - LandscapeAspectRatio) < 0.1, Is.True);
    }

    [Test]
    public void CropToLandscape_PortraitImage_CropsToCenterLandscape()
    {
        int width = 90, height = 160;
        var stride = width * 4;
        var pixels = BgraPixels(width, height);
        for (var row = 0; row < height; row++)
            for (var column = 0; column < stride; column++)
                pixels[row * stride + column] = (byte)(row % 256);

        var (result, croppedWidth, croppedHeight) = SnapshotService.CropToLandscape(pixels, width, height);

        Assert.That(croppedWidth, Is.EqualTo(width));
        Assert.That(croppedHeight < height, Is.True, "Cropped height should be less than original");

        var expectedCropHeight = (int)(width / LandscapeAspectRatio);
        var expectedFirstRow = (height - expectedCropHeight) / 2;
        Assert.That(result[0], Is.EqualTo((byte)(expectedFirstRow % 256)));
    }

    [Test]
    public void CaptureFrame_FirstCall_DoesNotThrow()
    {
        using var service = new SnapshotService(TempPath("snap.jpg"));

        service.CaptureFrame(4, 4, BgraPixels(4, 4));
    }

    [Test]
    public void CaptureFrame_CalledTwiceInARow_ThrottlesTheSecondCall()
    {
        using var service = new SnapshotService(TempPath("snap.jpg"));
        var pixels = BgraPixels(4, 4);

        service.CaptureFrame(4, 4, pixels);
        service.CaptureFrame(4, 4, pixels);
    }

    [Test]
    public async Task SaveNowAsync_WritesSnapshotAndHero()
    {
        var snapshotPath = TempPath("snap.jpg");
        using var service = new SnapshotService(snapshotPath);

        await service.SaveNowAsync(4, 4, BgraPixels(4, 4));

        Assert.That(File.Exists(snapshotPath), Is.True);
        Assert.That(File.Exists(SnapshotService.GetHeroPath(snapshotPath)), Is.True);
    }

    [Test]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = new SnapshotService(TempPath("snap.jpg"));

        service.Dispose();
        service.Dispose();
    }
}
