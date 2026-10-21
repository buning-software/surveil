using System;
using System.Linq;
using NUnit.Framework;

namespace Surveil.Unifi.Tests;

[TestFixture]
public sealed class ProtectEventStreamParseTests
{
    private static string EventJson(
        string type,
        string updateType = "add",
        string? end = null,
        string? smartDetectTypes = null,
        string? metadata = null)
    {
        var optionalFields = string.Empty;
        if (end is not null) optionalFields += ""","end":""" + end;
        if (smartDetectTypes is not null) optionalFields += ""","smartDetectTypes":""" + smartDetectTypes;
        if (metadata is not null) optionalFields += ""","metadata":""" + metadata;

        return $$$"""{"type":"{{{updateType}}}","item":{"id":"ev1","type":"{{{type}}}","start":1000{{{optionalFields}}},"device":"dev1"}}""";
    }

    [Test]
    public void ParseEvent_Motion_ReturnsMotionEvent()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("motion"));

        Assert.That(result, Is.InstanceOf<MotionEvent>());
        var ev = (MotionEvent)result;
        Assert.That(ev.Id, Is.EqualTo("ev1"));
        Assert.That(ev.Start, Is.EqualTo(1000L));
        Assert.That(ev.DeviceId, Is.EqualTo("dev1"));
        Assert.That(ev.UpdateType, Is.EqualTo(ProtectEventUpdateType.Add));
        Assert.That(ev.End, Is.Null);
    }

    [Test]
    public void ParseEvent_MotionWithEnd_HasEndTimestamp()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("motion", updateType: "update", end: "2000"));

        Assert.That(result, Is.InstanceOf<MotionEvent>());
        var ev = (MotionEvent)result;
        Assert.That(ev.End, Is.EqualTo(2000L));
        Assert.That(ev.UpdateType, Is.EqualTo(ProtectEventUpdateType.Update));
    }

    [Test]
    public void ParseEvent_NullEndProperty_ReturnsNullEnd()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("motion", end: "null"));

        Assert.That(result, Is.InstanceOf<MotionEvent>());
        var ev = (MotionEvent)result;
        Assert.That(ev.End, Is.Null);
    }

    [Test]
    public void ParseEvent_SmartDetectZone_ReturnsWithSmartTypes()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("smartDetectZone", smartDetectTypes: """["person","vehicle"]"""));

        Assert.That(result, Is.InstanceOf<SmartDetectZoneEvent>());
        var ev = (SmartDetectZoneEvent)result;
        Assert.That(ev.SmartDetectTypes.ToArray(), Is.EqualTo(new[] { "person", "vehicle" }));
    }

    [TestCase("smartDetectLine", """["car"]""", typeof(SmartDetectLineEvent))]
    [TestCase("smartDetectLoiterZone", "[]", typeof(SmartDetectLoiterZoneEvent))]
    [TestCase("smartAudioDetect", """["smoke"]""", typeof(SmartAudioDetectEvent))]
    [TestCase("ring", null, typeof(RingEvent))]
    [TestCase("lightMotion", null, typeof(LightMotionEvent))]
    [TestCase("sensorMotion", null, typeof(SensorMotionEvent))]
    [TestCase("sensorTamper", null, typeof(SensorTamperEvent))]
    [TestCase("sensorSmokeTest", null, typeof(SensorSmokeTestEvent))]
    public void ParseEvent_KnownType_ReturnsMatchingEvent(string type, string? smartDetectTypes, Type expected)
    {
        var result = ProtectEventStream.ParseEvent(EventJson(type, smartDetectTypes: smartDetectTypes));

        Assert.That(result, Is.InstanceOf(expected));
    }

    [Test]
    public void ParseEvent_SensorAlarm_WithMetadata_ReturnsAlarmType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorAlarm", metadata: """{"alarmType":{"text":"smoke"}}"""));

        Assert.That(result, Is.InstanceOf<SensorAlarmEvent>());
        var ev = (SensorAlarmEvent)result;
        Assert.That(ev.AlarmType, Is.EqualTo("smoke"));
    }

    [Test]
    public void ParseEvent_SensorAlarm_WithoutMetadata_ReturnsEmptyAlarmType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorAlarm"));

        Assert.That(result, Is.InstanceOf<SensorAlarmEvent>());
        var ev = (SensorAlarmEvent)result;
        Assert.That(ev.AlarmType, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ParseEvent_SensorOpened_WithMountType_ReturnsMountType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorOpened", metadata: """{"sensorMountType":{"text":"door"}}"""));

        Assert.That(result, Is.InstanceOf<SensorOpenedEvent>());
        var ev = (SensorOpenedEvent)result;
        Assert.That(ev.MountType, Is.EqualTo("door"));
    }

    [Test]
    public void ParseEvent_SensorOpened_WithoutMetadata_ReturnsEmptyMountType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorOpened"));

        Assert.That(result, Is.InstanceOf<SensorOpenedEvent>());
        var ev = (SensorOpenedEvent)result;
        Assert.That(ev.MountType, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ParseEvent_SensorClosed_WithMountType_ReturnsMountType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorClosed", metadata: """{"sensorMountType":{"text":"window"}}"""));

        Assert.That(result, Is.InstanceOf<SensorClosedEvent>());
        var ev = (SensorClosedEvent)result;
        Assert.That(ev.MountType, Is.EqualTo("window"));
    }

    [Test]
    public void ParseEvent_SensorClosed_WithoutMetadata_ReturnsEmptyMountType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorClosed"));

        Assert.That(result, Is.InstanceOf<SensorClosedEvent>());
        var ev = (SensorClosedEvent)result;
        Assert.That(ev.MountType, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ParseEvent_SensorWaterLeak_WithMountType_ReturnsMountType()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorWaterLeak", metadata: """{"sensorMountType":{"text":"leak"}}"""));

        Assert.That(result, Is.InstanceOf<SensorWaterLeakEvent>());
        var ev = (SensorWaterLeakEvent)result;
        Assert.That(ev.MountType, Is.EqualTo("leak"));
    }

    [Test]
    public void ParseEvent_SensorWaterLeak_WithoutMetadata_ReturnsEmptyMountType()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorWaterLeak"));

        Assert.That(result, Is.InstanceOf<SensorWaterLeakEvent>());
        var ev = (SensorWaterLeakEvent)result;
        Assert.That(ev.MountType, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ParseEvent_SensorBatteryLow_WithPercentage_ReturnsBatteryPercentage()
    {
        var result = ProtectEventStream.ParseEvent(
            EventJson("sensorBatteryLow", metadata: """{"sensorBatteryPercentage":{"number":12.5}}"""));

        Assert.That(result, Is.InstanceOf<SensorBatteryLowEvent>());
        var ev = (SensorBatteryLowEvent)result;
        Assert.That(ev.BatteryPercentage, Is.EqualTo(12.5));
    }

    [Test]
    public void ParseEvent_SensorBatteryLow_WithoutMetadata_ReturnsZeroPercentage()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorBatteryLow"));

        Assert.That(result, Is.InstanceOf<SensorBatteryLowEvent>());
        var ev = (SensorBatteryLowEvent)result;
        Assert.That(ev.BatteryPercentage, Is.EqualTo(0d));
    }

    [Test]
    public void ParseEvent_SensorExtremeValues_WithAllMetadata_ReturnsAllFields()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorExtremeValues",
            metadata: """{"sensorType":{"text":"temperature"},"sensorValue":{"text":42.5},"status":{"text":"high"}}"""));

        Assert.That(result, Is.InstanceOf<SensorExtremeValuesEvent>());
        var ev = (SensorExtremeValuesEvent)result;
        Assert.That(ev.SensorType, Is.EqualTo("temperature"));
        Assert.That(ev.SensorValue, Is.EqualTo(42.5));
        Assert.That(ev.Status, Is.EqualTo("high"));
    }

    [Test]
    public void ParseEvent_SensorExtremeValues_WithoutMetadata_ReturnsDefaults()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("sensorExtremeValues"));

        Assert.That(result, Is.InstanceOf<SensorExtremeValuesEvent>());
        var ev = (SensorExtremeValuesEvent)result;
        Assert.That(ev.SensorType, Is.EqualTo(string.Empty));
        Assert.That(ev.SensorValue, Is.EqualTo(0d));
        Assert.That(ev.Status, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ParseEvent_UnknownType_ReturnsUnknownEvent()
    {
        var result = ProtectEventStream.ParseEvent(EventJson("mystery"));

        Assert.That(result, Is.InstanceOf<UnknownEvent>());
        var ev = (UnknownEvent)result;
        Assert.That(ev.Type, Is.EqualTo("mystery"));
    }

    [Test]
    public void ParseEvent_InvalidJson_ReturnsNull()
    {
        Assert.That(ProtectEventStream.ParseEvent("not-valid-json"), Is.Null);
    }

    [Test]
    public void ParseEvent_MissingItemProperty_ReturnsNull()
    {
        Assert.That(ProtectEventStream.ParseEvent("""{"type":"add"}"""), Is.Null);
    }
}
