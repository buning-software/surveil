using NUnit.Framework;
using Surveil.Domain.Cameras;

namespace Surveil.Core.Tests.Domain;

[TestFixture]
public sealed class RtspsStreamTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var stream = new RtspsStream("rtsps://host/stream", "high");

        Assert.That(stream.Url, Is.EqualTo("rtsps://host/stream"));
        Assert.That(stream.StreamName, Is.EqualTo("high"));
    }

    [Test]
    public void Equality_ComparesByValue()
    {
        var stream = new RtspsStream("rtsps://host/stream", "high");
        var same = new RtspsStream("rtsps://host/stream", "high");
        var other = new RtspsStream("rtsps://host/other", "high");

        Assert.That(same, Is.EqualTo(stream));
        Assert.That(other, Is.Not.EqualTo(stream));
    }
}
