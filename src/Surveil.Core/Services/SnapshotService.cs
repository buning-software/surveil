using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Surveil.Services;

public sealed class SnapshotService : IDisposable
{
    private const double HeroAspectRatio = 16.0 / 9.0;

    private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(5);

    private readonly string _snapshotPath;
    private readonly string _heroPath;
    private readonly bool _enabled;
    private long _nextSaveTicks;
    private int _saving;

    public SnapshotService(string snapshotPath)
    {
        _snapshotPath = snapshotPath;
        _heroPath = GetHeroPath(snapshotPath);
        _nextSaveTicks = DateTime.UtcNow.Ticks;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_snapshotPath)!);
            _enabled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SnapshotService] Snapshot directory unavailable, snapshots disabled: {ex.Message}");
        }
    }

    public static string GetHeroPath(string snapshotPath) =>
        Path.Combine(
            Path.GetDirectoryName(snapshotPath)!,
            Path.GetFileNameWithoutExtension(snapshotPath) + "-hero" + Path.GetExtension(snapshotPath));

    public void CaptureFrame(int width, int height, byte[] pixels)
    {
        if (!_enabled) return;

        var now = DateTime.UtcNow.Ticks;
        if (now < Interlocked.Read(ref _nextSaveTicks)) return;
        if (Interlocked.CompareExchange(ref _saving, 1, 0) != 0) return;

        Interlocked.Exchange(ref _nextSaveTicks, now + SaveInterval.Ticks);

        var copy = new byte[pixels.Length];
        Buffer.BlockCopy(pixels, 0, copy, 0, pixels.Length);

        _ = Task.Run(async () =>
        {
            try { await SaveAsync(width, height, copy); }
            catch (Exception ex) { Debug.WriteLine($"Snapshot failed: {ex.Message}"); }
            finally { Interlocked.Exchange(ref _saving, 0); }
        });
    }

    public void Dispose() { }

    public Task SaveNowAsync(int width, int height, byte[] pixels) =>
        _enabled ? SaveAsync(width, height, pixels) : Task.CompletedTask;

    internal async Task SaveAsync(int width, int height, byte[] pixels)
    {
        await SaveJpegAsync(_snapshotPath, width, height, pixels);

        var (heroPixels, heroWidth, heroHeight) = CropToLandscape(pixels, width, height);
        await SaveJpegAsync(_heroPath, heroWidth, heroHeight, heroPixels);
    }

    internal static (byte[] pixels, int width, int height) CropToLandscape(byte[] pixels, int width, int height)
    {
        var currentAspect = (double)width / height;
        if (currentAspect >= HeroAspectRatio)
            return (pixels, width, height);

        var cropHeight = (int)(width / HeroAspectRatio);
        var startY = (height - cropHeight) / 2;
        var stride = width * 4;

        var cropped = new byte[stride * cropHeight];
        Buffer.BlockCopy(pixels, startY * stride, cropped, 0, cropped.Length);

        return (cropped, width, cropHeight);
    }

    private static async Task SaveJpegAsync(string path, int width, int height, byte[] pixels)
    {
        using var ras = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ras);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            (uint)width, (uint)height,
            96, 96,
            pixels);
        await encoder.FlushAsync();

        ras.Seek(0);
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        await ras.AsStreamForRead().CopyToAsync(fileStream);
    }
}
