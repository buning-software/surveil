using NUnit.Framework;

namespace Surveil.Unifi.Tests;

[TestFixture]
public sealed class UnifiProtectOptionsTests
{
    [Test]
    public void Options_Properties_AreSetViaInitializer()
    {
        var options = new UnifiProtectOptions
        {
            BaseUrl = "https://nvr.example.invalid/proxy/protect/api",
            ApiKey = "fake-api-key"
        };

        Assert.That(options.BaseUrl, Is.EqualTo("https://nvr.example.invalid/proxy/protect/api"));
        Assert.That(options.ApiKey, Is.EqualTo("fake-api-key"));
    }
}
