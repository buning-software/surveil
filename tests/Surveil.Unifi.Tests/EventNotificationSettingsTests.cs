using NUnit.Framework;

namespace Surveil.Unifi.Tests;

[TestFixture]
public sealed class EventNotificationSettingsTests
{
    private static MotionEvent Motion() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SmartDetectZoneEvent SmartDetectZone() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static SmartDetectLineEvent SmartDetectLine() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static SmartDetectLoiterZoneEvent SmartDetectLoiterZone() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static SmartAudioDetectEvent SmartAudioDetect() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, []);
    private static RingEvent Ring() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static LightMotionEvent LightMotion() => new("id", 0, "dev", ProtectEventUpdateType.Add);
    private static SensorMotionEvent SensorMotion() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SensorTamperEvent SensorTamper() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SensorSmokeTestEvent SensorSmokeTest() => new("id", 0, null, "dev", ProtectEventUpdateType.Add);
    private static SensorAlarmEvent SensorAlarm() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "smoke");
    private static SensorOpenedEvent SensorOpened() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "door");
    private static SensorClosedEvent SensorClosed() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "window");
    private static SensorWaterLeakEvent SensorWaterLeak() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "leak");
    private static SensorBatteryLowEvent SensorBatteryLow() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, 15.0);
    private static SensorExtremeValuesEvent SensorExtremeValues() => new("id", 0, null, "dev", ProtectEventUpdateType.Add, "temperature", 45.0, "high");
    private static UnknownEvent Unknown() => new("id", "weird", 0, null, "dev", ProtectEventUpdateType.Add);

    [Test]
    public void Defaults_OnlyRingIsTrue()
    {
        var settings = new EventNotificationSettings();

        Assert.That(settings.Motion, Is.False);
        Assert.That(settings.SmartDetectZone, Is.False);
        Assert.That(settings.SmartDetectLine, Is.False);
        Assert.That(settings.SmartDetectLoiterZone, Is.False);
        Assert.That(settings.SmartAudioDetect, Is.False);
        Assert.That(settings.Ring, Is.True);
        Assert.That(settings.LightMotion, Is.False);
        Assert.That(settings.SensorMotion, Is.False);
        Assert.That(settings.SensorTamper, Is.False);
        Assert.That(settings.SensorSmokeTest, Is.False);
        Assert.That(settings.SensorAlarm, Is.False);
        Assert.That(settings.SensorOpened, Is.False);
        Assert.That(settings.SensorClosed, Is.False);
        Assert.That(settings.SensorWaterLeak, Is.False);
        Assert.That(settings.SensorBatteryLow, Is.False);
        Assert.That(settings.SensorExtremeValues, Is.False);
    }

    [Test]
    public void IsEnabled_MotionDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(Motion()), Is.False);

    [Test]
    public void IsEnabled_MotionEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { Motion = true }.IsEnabled(Motion()), Is.True);

    [Test]
    public void IsEnabled_SmartDetectZoneDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SmartDetectZone()), Is.False);

    [Test]
    public void IsEnabled_SmartDetectZoneEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SmartDetectZone = true }.IsEnabled(SmartDetectZone()), Is.True);

    [Test]
    public void IsEnabled_SmartDetectLineDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SmartDetectLine()), Is.False);

    [Test]
    public void IsEnabled_SmartDetectLineEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SmartDetectLine = true }.IsEnabled(SmartDetectLine()), Is.True);

    [Test]
    public void IsEnabled_SmartDetectLoiterZoneDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SmartDetectLoiterZone()), Is.False);

    [Test]
    public void IsEnabled_SmartDetectLoiterZoneEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SmartDetectLoiterZone = true }.IsEnabled(SmartDetectLoiterZone()), Is.True);

    [Test]
    public void IsEnabled_SmartAudioDetectDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SmartAudioDetect()), Is.False);

    [Test]
    public void IsEnabled_SmartAudioDetectEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SmartAudioDetect = true }.IsEnabled(SmartAudioDetect()), Is.True);

    [Test]
    public void IsEnabled_RingEnabledByDefault_ReturnsTrue()
        => Assert.That(new EventNotificationSettings().IsEnabled(Ring()), Is.True);

    [Test]
    public void IsEnabled_RingDisabled_ReturnsFalse()
        => Assert.That(new EventNotificationSettings { Ring = false }.IsEnabled(Ring()), Is.False);

    [Test]
    public void IsEnabled_LightMotionDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(LightMotion()), Is.False);

    [Test]
    public void IsEnabled_LightMotionEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { LightMotion = true }.IsEnabled(LightMotion()), Is.True);

    [Test]
    public void IsEnabled_SensorMotionDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorMotion()), Is.False);

    [Test]
    public void IsEnabled_SensorMotionEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorMotion = true }.IsEnabled(SensorMotion()), Is.True);

    [Test]
    public void IsEnabled_SensorTamperDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorTamper()), Is.False);

    [Test]
    public void IsEnabled_SensorTamperEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorTamper = true }.IsEnabled(SensorTamper()), Is.True);

    [Test]
    public void IsEnabled_SensorSmokeTestDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorSmokeTest()), Is.False);

    [Test]
    public void IsEnabled_SensorSmokeTestEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorSmokeTest = true }.IsEnabled(SensorSmokeTest()), Is.True);

    [Test]
    public void IsEnabled_SensorAlarmDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorAlarm()), Is.False);

    [Test]
    public void IsEnabled_SensorAlarmEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorAlarm = true }.IsEnabled(SensorAlarm()), Is.True);

    [Test]
    public void IsEnabled_SensorOpenedDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorOpened()), Is.False);

    [Test]
    public void IsEnabled_SensorOpenedEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorOpened = true }.IsEnabled(SensorOpened()), Is.True);

    [Test]
    public void IsEnabled_SensorClosedDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorClosed()), Is.False);

    [Test]
    public void IsEnabled_SensorClosedEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorClosed = true }.IsEnabled(SensorClosed()), Is.True);

    [Test]
    public void IsEnabled_SensorWaterLeakDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorWaterLeak()), Is.False);

    [Test]
    public void IsEnabled_SensorWaterLeakEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorWaterLeak = true }.IsEnabled(SensorWaterLeak()), Is.True);

    [Test]
    public void IsEnabled_SensorBatteryLowDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorBatteryLow()), Is.False);

    [Test]
    public void IsEnabled_SensorBatteryLowEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorBatteryLow = true }.IsEnabled(SensorBatteryLow()), Is.True);

    [Test]
    public void IsEnabled_SensorExtremeValuesDisabledByDefault_ReturnsFalse()
        => Assert.That(new EventNotificationSettings().IsEnabled(SensorExtremeValues()), Is.False);

    [Test]
    public void IsEnabled_SensorExtremeValuesEnabled_ReturnsTrue()
        => Assert.That(new EventNotificationSettings { SensorExtremeValues = true }.IsEnabled(SensorExtremeValues()), Is.True);

    [Test]
    public void IsEnabled_UnknownEvent_ReturnsFalse()
        => Assert.That(new EventNotificationSettings { Ring = true }.IsEnabled(Unknown()), Is.False);
}
