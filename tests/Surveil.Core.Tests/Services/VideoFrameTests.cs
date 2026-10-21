using System.Buffers;
using NUnit.Framework;
using Surveil.Services;

namespace Surveil.Core.Tests.Services;

[TestFixture]
public sealed class VideoFrameTests
{
    private static VideoFrame RentFrame(int width = 4, int height = 4)
    {
        var dataLength = width * height * 4;
        var pixels = ArrayPool<byte>.Shared.Rent(dataLength);
        return new VideoFrame(pixels, width, height, dataLength);
    }

    [Test]
    public void Constructor_SetsAllProperties()
    {
        using var frame = RentFrame(8, 6);

        Assert.That(frame.Width, Is.EqualTo(8));
        Assert.That(frame.Height, Is.EqualTo(6));
        Assert.That(frame.DataLength, Is.EqualTo(8 * 6 * 4));
        Assert.That(frame.Pixels, Is.Not.Null);
    }

    [Test]
    public void Dispose_ReturnsPixelsToThePool()
    {
        var frame = RentFrame();

        frame.Dispose();
    }

    [Test]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var frame = RentFrame();

        frame.Dispose();
        frame.Dispose();
    }
}
