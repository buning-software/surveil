using System.Linq;
using NUnit.Framework;

namespace Surveil.Unifi.Tests;

[TestFixture]
public sealed class ProtectEventTests
{
    private const string Id = "event-1";
    private const string DeviceId = "device-1";
    private const long Start = 1_700_000_000_000L;
    private const long End = 1_700_000_005_000L;

    [Test]
    public void MotionEvent_TypeIsMotion()
    {
        var ev = new MotionEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add);

        Assert.That(ev.Type, Is.EqualTo("motion"));
        Assert.That(ev.Id, Is.EqualTo(Id));
        Assert.That(ev.Start, Is.EqualTo(Start));
        Assert.That(ev.End, Is.EqualTo(End));
        Assert.That(ev.DeviceId, Is.EqualTo(DeviceId));
        Assert.That(ev.UpdateType, Is.EqualTo(ProtectEventUpdateType.Add));
    }

    [Test]
    public void MotionEvent_WithNullEnd_HasNullEnd()
    {
        var ev = new MotionEvent(Id, Start, null, DeviceId, ProtectEventUpdateType.Update);

        Assert.That(ev.End, Is.Null);
    }

    [Test]
    public void SmartDetectZoneEvent_TypeAndSmartDetectTypes()
    {
        var ev = new SmartDetectZoneEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, ["person", "vehicle"]);

        Assert.That(ev.Type, Is.EqualTo("smartDetectZone"));
        Assert.That(ev.SmartDetectTypes.ToArray(), Is.EqualTo(new[] { "person", "vehicle" }));
    }

    [Test]
    public void SmartDetectLineEvent_TypeIsSmartDetectLine()
    {
        var ev = new SmartDetectLineEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, ["car"]);

        Assert.That(ev.Type, Is.EqualTo("smartDetectLine"));
        Assert.That(ev.SmartDetectTypes.Count, Is.EqualTo(1));
    }

    [Test]
    public void SmartDetectLoiterZoneEvent_TypeIsSmartDetectLoiterZone()
    {
        var ev = new SmartDetectLoiterZoneEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Update, []);

        Assert.That(ev.Type, Is.EqualTo("smartDetectLoiterZone"));
        Assert.That(ev.SmartDetectTypes, Is.Empty);
    }

    [Test]
    public void SmartAudioDetectEvent_TypeIsSmartAudioDetect()
    {
        var ev = new SmartAudioDetectEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, ["smoke"]);

        Assert.That(ev.Type, Is.EqualTo("smartAudioDetect"));
    }

    [Test]
    public void RingEvent_TypeIsRing()
    {
        var ev = new RingEvent(Id, Start, null, DeviceId, ProtectEventUpdateType.Add);

        Assert.That(ev.Type, Is.EqualTo("ring"));
        Assert.That(ev.End, Is.Null);
    }

    [Test]
    public void LightMotionEvent_TypeIsLightMotionAndEndIsNull()
    {
        var ev = new LightMotionEvent(Id, Start, DeviceId, ProtectEventUpdateType.Add);

        Assert.That(ev.Type, Is.EqualTo("lightMotion"));
        Assert.That(ev.End, Is.Null);
    }

    [Test]
    public void SensorMotionEvent_TypeIsSensorMotion()
    {
        var ev = new SensorMotionEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add);

        Assert.That(ev.Type, Is.EqualTo("sensorMotion"));
    }

    [Test]
    public void SensorTamperEvent_TypeIsSensorTamper()
    {
        var ev = new SensorTamperEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Update);

        Assert.That(ev.Type, Is.EqualTo("sensorTamper"));
    }

    [Test]
    public void SensorSmokeTestEvent_TypeIsSensorSmokeTest()
    {
        var ev = new SensorSmokeTestEvent(Id, Start, null, DeviceId, ProtectEventUpdateType.Add);

        Assert.That(ev.Type, Is.EqualTo("sensorSmokeTest"));
    }

    [Test]
    public void SensorAlarmEvent_TypeAndAlarmType()
    {
        var ev = new SensorAlarmEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "smoke");

        Assert.That(ev.Type, Is.EqualTo("sensorAlarm"));
        Assert.That(ev.AlarmType, Is.EqualTo("smoke"));
    }

    [Test]
    public void SensorOpenedEvent_TypeAndMountType()
    {
        var ev = new SensorOpenedEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "door");

        Assert.That(ev.Type, Is.EqualTo("sensorOpened"));
        Assert.That(ev.MountType, Is.EqualTo("door"));
    }

    [Test]
    public void SensorClosedEvent_TypeAndMountType()
    {
        var ev = new SensorClosedEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "window");

        Assert.That(ev.Type, Is.EqualTo("sensorClosed"));
        Assert.That(ev.MountType, Is.EqualTo("window"));
    }

    [Test]
    public void SensorWaterLeakEvent_TypeAndMountType()
    {
        var ev = new SensorWaterLeakEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "leak");

        Assert.That(ev.Type, Is.EqualTo("sensorWaterLeak"));
        Assert.That(ev.MountType, Is.EqualTo("leak"));
    }

    [Test]
    public void SensorBatteryLowEvent_TypeAndBatteryPercentage()
    {
        var ev = new SensorBatteryLowEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, 12.5);

        Assert.That(ev.Type, Is.EqualTo("sensorBatteryLow"));
        Assert.That(ev.BatteryPercentage, Is.EqualTo(12.5));
    }

    [Test]
    public void SensorExtremeValuesEvent_AllFields()
    {
        var ev = new SensorExtremeValuesEvent(Id, Start, End, DeviceId, ProtectEventUpdateType.Add, "temperature", 42.1, "high");

        Assert.That(ev.Type, Is.EqualTo("sensorExtremeValues"));
        Assert.That(ev.SensorType, Is.EqualTo("temperature"));
        Assert.That(ev.SensorValue, Is.EqualTo(42.1));
        Assert.That(ev.Status, Is.EqualTo("high"));
    }

    [Test]
    public void UnknownEvent_TypeReflectsRawType()
    {
        var ev = new UnknownEvent(Id, "customType", Start, End, DeviceId, ProtectEventUpdateType.Add);

        Assert.That(ev.Type, Is.EqualTo("customType"));
        Assert.That(ev.Id, Is.EqualTo(Id));
    }

    [Test]
    public void ProtectEventUpdateType_Values()
    {
        Assert.That((int)ProtectEventUpdateType.Add, Is.EqualTo(0));
        Assert.That((int)ProtectEventUpdateType.Update, Is.EqualTo(1));
    }
}
