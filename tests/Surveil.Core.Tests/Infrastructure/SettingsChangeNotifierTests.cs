using NUnit.Framework;
using Surveil.Application.Settings;
using Surveil.Infrastructure.Settings;

namespace Surveil.Core.Tests.Infrastructure;

[TestFixture]
public sealed class SettingsChangeNotifierTests
{
    [Test]
    public void NotifyChanged_InvokesSubscribersWithSettings()
    {
        var notifier = new SettingsChangeNotifier();
        AppSettings? received = null;
        notifier.SettingsChanged += settings => received = settings;

        var sent = new AppSettings { SelectedProvider = VideoProviderType.UnifiProtect };
        notifier.NotifyChanged(sent);

        Assert.That(received, Is.SameAs(sent));
    }

    [Test]
    public void NotifyChanged_NoSubscribers_DoesNotThrow()
    {
        var notifier = new SettingsChangeNotifier();
        notifier.NotifyChanged(new AppSettings());
    }
}
