using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestClass]
public sealed class SnapshotServiceTests
{
    private const double LandscapeAspectRatio = 16.0 / 9.0;

    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"snapshot-tests-{Guid.NewGuid():N}");

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(params string[] segments) =>
        Path.Combine([_tempDir, .. segments]);

    private static byte[] BgraPixels(int width, int height) => new byte[width * height * 4];

    [TestMethod]
    public void GetHeroPath_AddsHeroSuffix()
    {
        var hero = SnapshotService.GetHeroPath(@"C:\snapshots\snapshot.jpg");

        Assert.AreEqual(@"C:\snapshots\snapshot-hero.jpg", hero);
    }

    [TestMethod]
    public void GetHeroPath_PreservesExtension()
    {
        var hero = SnapshotService.GetHeroPath(@"C:\snapshots\frame.png");

        Assert.IsTrue(hero.EndsWith("-hero.png"));
    }

    [TestMethod]
    public void GetHeroPath_NestedDirectory_HeroIsInSameDirectory()
    {
        var path = TempPath("sub", "shot.jpg");

        var hero = SnapshotService.GetHeroPath(path);

        Assert.AreEqual(Path.GetDirectoryName(path), Path.GetDirectoryName(hero));
        Assert.IsTrue(Path.GetFileName(hero).StartsWith("shot-hero"));
    }

    [TestMethod]
    public void Constructor_CreatesDirectory()
    {
        var snapshotPath = TempPath("shots", "snapshot.jpg");

        using var service = new SnapshotService(snapshotPath);

        Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(snapshotPath)));
    }

    [TestMethod]
    [DataRow(1920, 1080)]
    [DataRow(160, 90)]
    public void CropToLandscape_WhenAlreadyLandscape_ReturnsSamePixels(int width, int height)
    {
        var pixels = BgraPixels(width, height);

        var (result, croppedWidth, croppedHeight) = SnapshotService.CropToLandscape(pixels, width, height);

        Assert.AreSame(pixels, result);
        Assert.AreEqual(width, croppedWidth);
        Assert.AreEqual(height, croppedHeight);
    }

    [TestMethod]
    public void CropToLandscape_SquareImage_CropsToLandscape()
    {
        var pixels = BgraPixels(100, 100);

        var (_, croppedWidth, croppedHeight) = SnapshotService.CropToLandscape(pixels, 100, 100);

        Assert.AreEqual(100, croppedWidth);
        Assert.IsTrue(croppedHeight < 100, "Cropped height should be less than original");
        Assert.IsTrue(Math.Abs((double)croppedWidth / croppedHeight - LandscapeAspectRatio) < 0.1);
    }

    [TestMethod]
    public void CropToLandscape_PortraitImage_CropsToCenterLandscape()
    {
        int width = 90, height = 160;
        var stride = width * 4;
        var pixels = BgraPixels(width, height);
        for (var row = 0; row < height; row++)
            for (var column = 0; column < stride; column++)
                pixels[row * stride + column] = (byte)(row % 256);

        var (result, croppedWidth, croppedHeight) = SnapshotService.CropToLandscape(pixels, width, height);

        Assert.AreEqual(width, croppedWidth);
        Assert.IsTrue(croppedHeight < height, "Cropped height should be less than original");

        var expectedCropHeight = (int)(width / LandscapeAspectRatio);
        var expectedFirstRow = (height - expectedCropHeight) / 2;
        Assert.AreEqual((byte)(expectedFirstRow % 256), result[0]);
    }

    [TestMethod]
    public void CaptureFrame_FirstCall_DoesNotThrow()
    {
        using var service = new SnapshotService(TempPath("snap.jpg"));

        service.CaptureFrame(4, 4, BgraPixels(4, 4));
    }

    [TestMethod]
    public void CaptureFrame_CalledTwiceInARow_ThrottlesTheSecondCall()
    {
        using var service = new SnapshotService(TempPath("snap.jpg"));
        var pixels = BgraPixels(4, 4);

        service.CaptureFrame(4, 4, pixels);
        service.CaptureFrame(4, 4, pixels);
    }

    [TestMethod]
    public async Task SaveNowAsync_WritesSnapshotAndHero()
    {
        var snapshotPath = TempPath("snap.jpg");
        using var service = new SnapshotService(snapshotPath);

        await service.SaveNowAsync(4, 4, BgraPixels(4, 4));

        Assert.IsTrue(File.Exists(snapshotPath));
        Assert.IsTrue(File.Exists(SnapshotService.GetHeroPath(snapshotPath)));
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = new SnapshotService(TempPath("snap.jpg"));

        service.Dispose();
        service.Dispose();
    }
}
