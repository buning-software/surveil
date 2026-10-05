using System.IO;
using NUnit.Framework;
using Surveil.UiTests.Application;
using WinUia;
using static WinUia.NUnit.UiAssertions;

namespace Surveil.UiTests.Tests;

[TestFixture]
public class SettingsPageTests
{
    private const string NotSavedTitle = "Not saved";
    private const string BaseUrlRequired = "Base URL is required.";
    private const string BaseUrlInvalid = "Base URL must be a valid absolute URL (e.g. https://192.168.0.1).";
    private const string ApiKeyRequired = "API Key is required.";

    private const string ValidBaseUrl = "https://nvr.example.invalid";
    private const string FakeApiKey = "fake-api-key";

    private SurveilApp _app = null!;
    private SettingsPage _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _app = App.Launch<SurveilApp>();
        _settings = _app.Shell.OpenSettings();
    }

    [TearDown]
    public void TearDown() => _app.Dispose();

    [Test]
    public void OpenSettings_ShowsTitle()
    {
        Assert.That(_settings.Title.Name, Is.EqualTo("Settings"));
    }

    [Test]
    public void OpenSettings_KeepsMenuAvailable()
    {
        Assert.That(_settings.CamerasItem.Name, Is.EqualTo("Cameras"));
    }

    [TestCase("General")]
    [TestCase("Video provider")]
    public void OpenSettings_ShowsSectionHeader(string header)
    {
        Assert.That(_settings.SectionHeader(header).Name, Is.EqualTo(header));
    }

    [Test]
    public void FreshSettings_ShowsNoError()
    {
        Assert.That(_settings.ErrorMessage, Is.Null);
    }

    [Test]
    public void FreshSettings_ProviderIsNone()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.ReadSelectedProvider(), Is.EqualTo("None"));
            Assert.That(_settings.ErrorMessage, Is.Null);
        }
    }

    [Test]
    public void FreshSettings_HidesUnifiConnection()
    {
        Assert.That(_settings.IsUnifiConnectionShown, Is.False);
    }

    [Test]
    public void SelectingUnifiProtect_ShowsUnifiConnection()
    {
        _settings.SelectProvider("UniFi Protect");

        Eventually(() => _settings.IsUnifiConnectionShown, "the UniFi Protect connection section appears for that provider");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.ApiKeyTextBox.IsEnabled, Is.True);
            Assert.That(_settings.SnapshotPathTextBox.IsEnabled, Is.True);
        }
    }

    [Test]
    public void SelectingUnifiProtect_WithoutBaseUrl_ShowsBaseUrlRequired()
    {
        _settings.SelectProvider("UniFi Protect");

        Eventually(() => _settings.ErrorMessage == BaseUrlRequired, "an empty Base URL fails validation");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_settings.ErrorTitle, Is.EqualTo(NotSavedTitle));
            Assert.That(File.Exists(_app.SettingsFile), Is.False);
        }
    }

    [Test]
    public void EnteringRelativeBaseUrl_ShowsBaseUrlInvalid()
    {
        _settings.SelectProvider("UniFi Protect");

        _settings.EnterBaseUrl("nvr.example.invalid");

        Eventually(() => _settings.ErrorMessage == BaseUrlInvalid, "a Base URL without a scheme is not an absolute URL");
        Assert.That(File.Exists(_app.SettingsFile), Is.False);
    }

    [Test]
    public void EnteringBaseUrlWithoutApiKey_ShowsApiKeyRequired()
    {
        _settings.SelectProvider("UniFi Protect");

        _settings.EnterBaseUrl(ValidBaseUrl);

        Eventually(() => _settings.ErrorMessage == ApiKeyRequired, "a valid Base URL still needs an API key");
        Assert.That(File.Exists(_app.SettingsFile), Is.False);
    }

    [Test]
    public void EnteringValidUnifiConnection_ClearsErrorAndSavesToIsolatedDataDirectory()
    {
        _settings.SelectProvider("UniFi Protect");

        _settings.EnterUnifiConnection(ValidBaseUrl, FakeApiKey);

        Eventually(
            () => _app.TryReadSettingsFile()?.Contains(ValidBaseUrl) == true,
            "valid settings apply immediately and are written to SURVEIL_DATA_DIR");
        Eventually(() => _settings.ErrorMessage is null, "a successful save closes the error");
    }

    [Test]
    public void SwitchingBackToNone_ClearsError()
    {
        _settings.SelectProvider("UniFi Protect");
        Eventually(() => _settings.ErrorMessage == BaseUrlRequired, "an empty Base URL fails validation");

        _settings.SelectProvider("None");

        Eventually(() => _settings.ErrorMessage is null, "no provider needs no connection details");
        Assert.That(_settings.IsUnifiConnectionShown, Is.False);
    }
}
