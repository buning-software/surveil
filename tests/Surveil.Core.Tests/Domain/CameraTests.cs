using NUnit.Framework;
using Surveil.Domain.Cameras;

namespace Surveil.Core.Tests.Domain;

[TestFixture]
public sealed class CameraTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var camera = new Camera("id-1", "Front Door", true);

        Assert.That(camera.Id, Is.EqualTo("id-1"));
        Assert.That(camera.Name, Is.EqualTo("Front Door"));
        Assert.That(camera.IsConnected, Is.True);
    }

    [Test]
    public void Constructor_WithIsConnectedFalse_LeavesTheCameraDisconnected()
    {
        var camera = new Camera("id-2", "Backyard", false);

        Assert.That(camera.IsConnected, Is.False);
    }

    [Test]
    public void Equality_ComparesByValue()
    {
        var camera = new Camera("id-1", "Front Door", true);
        var same = new Camera("id-1", "Front Door", true);
        var other = new Camera("id-2", "Front Door", true);

        Assert.That(same, Is.EqualTo(camera));
        Assert.That(other, Is.Not.EqualTo(camera));
    }

    [Test]
    public void ToString_ContainsTheFieldValues()
    {
        var camera = new Camera("abc", "Garage", false);

        var text = camera.ToString();

        Assert.That(text, Is.Not.Null);
        Assert.That(text.Contains("abc"), Is.True);
    }
}
