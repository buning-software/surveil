using NUnit.Framework;

namespace Surveil.Unifi.Tests;

[TestFixture]
public sealed class ProtectEventDescriberTests
{
    private static string Title(ProtectEvent protectEvent, string cameraName) =>
        $"{ProtectEventStream.ToCameraEvent(protectEvent).Description} ({cameraName})";

    [Test]
    public void Title_MotionEvent_ContainsCameraName()
    {
        var ev = new MotionEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Front Door"), Is.EqualTo("Motion detected (Front Door)"));
    }

    [Test]
    public void Title_SmartDetectZoneWithTypes_FormatsTypes()
    {
        var ev = new SmartDetectZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["person", "vehicle"]);

        Assert.That(Title(ev, "Backyard"), Is.EqualTo("Person, Vehicle detected (Backyard)"));
    }

    [Test]
    public void Title_SmartDetectZoneWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.That(Title(ev, "Backyard"), Is.EqualTo("Smart detection (Backyard)"));
    }

    [Test]
    public void Title_SmartDetectLineWithTypes_FormatsLineMessage()
    {
        var ev = new SmartDetectLineEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["car"]);

        Assert.That(Title(ev, "Gate"), Is.EqualTo("Car crossed line (Gate)"));
    }

    [Test]
    public void Title_SmartDetectLineWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectLineEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.That(Title(ev, "Gate"), Is.EqualTo("Line crossing (Gate)"));
    }

    [Test]
    public void Title_SmartDetectLoiterZoneWithTypes_FormatsLoiteringMessage()
    {
        var ev = new SmartDetectLoiterZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["person"]);

        Assert.That(Title(ev, "Park"), Is.EqualTo("Person loitering (Park)"));
    }

    [Test]
    public void Title_SmartDetectLoiterZoneWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartDetectLoiterZoneEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.That(Title(ev, "Park"), Is.EqualTo("Loitering detected (Park)"));
    }

    [Test]
    public void Title_SmartAudioDetectWithTypes_FormatsAudioMessage()
    {
        var ev = new SmartAudioDetectEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, ["smoke"]);

        Assert.That(Title(ev, "Kitchen"), Is.EqualTo("Audio: Smoke (Kitchen)"));
    }

    [Test]
    public void Title_SmartAudioDetectWithoutTypes_UsesGenericMessage()
    {
        var ev = new SmartAudioDetectEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, []);

        Assert.That(Title(ev, "Kitchen"), Is.EqualTo("Audio detection (Kitchen)"));
    }

    [Test]
    public void Title_RingEvent_ShowsDoorbellMessage()
    {
        var ev = new RingEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Front Door"), Is.EqualTo("Doorbell ring (Front Door)"));
    }

    [Test]
    public void Title_LightMotionEvent_ShowsFloodlightMessage()
    {
        var ev = new LightMotionEvent("id", 0, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Porch"), Is.EqualTo("Floodlight motion (Porch)"));
    }

    [Test]
    public void Title_SensorMotionEvent_ShowsSensorMotion()
    {
        var ev = new SensorMotionEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Hallway"), Is.EqualTo("Sensor motion (Hallway)"));
    }

    [Test]
    public void Title_SensorTamperEvent_ShowsSensorTampered()
    {
        var ev = new SensorTamperEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Garage"), Is.EqualTo("Sensor tampered (Garage)"));
    }

    [Test]
    public void Title_SensorSmokeTestEvent_ShowsSmokeTest()
    {
        var ev = new SensorSmokeTestEvent("id", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Living Room"), Is.EqualTo("Smoke detector test (Living Room)"));
    }

    [Test]
    public void Title_SensorAlarmWithAlarmType_ShowsAlarmType()
    {
        var ev = new SensorAlarmEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "CO");

        Assert.That(Title(ev, "Bedroom"), Is.EqualTo("Sensor alarm: CO (Bedroom)"));
    }

    [Test]
    public void Title_SensorAlarmWithoutAlarmType_ShowsGenericAlarm()
    {
        var ev = new SensorAlarmEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");

        Assert.That(Title(ev, "Bedroom"), Is.EqualTo("Sensor alarm (Bedroom)"));
    }

    [Test]
    public void Title_SensorOpenedWithMountType_CapitalizesMountType()
    {
        var ev = new SensorOpenedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "door");

        Assert.That(Title(ev, "Entry"), Is.EqualTo("Door opened (Entry)"));
    }

    [Test]
    public void Title_SensorOpenedWithoutMountType_ShowsGenericOpened()
    {
        var ev = new SensorOpenedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");

        Assert.That(Title(ev, "Entry"), Is.EqualTo("Sensor opened (Entry)"));
    }

    [Test]
    public void Title_SensorClosedWithMountType_CapitalizesMountType()
    {
        var ev = new SensorClosedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "window");

        Assert.That(Title(ev, "Office"), Is.EqualTo("Window closed (Office)"));
    }

    [Test]
    public void Title_SensorClosedWithoutMountType_ShowsGenericClosed()
    {
        var ev = new SensorClosedEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "");

        Assert.That(Title(ev, "Office"), Is.EqualTo("Sensor closed (Office)"));
    }

    [Test]
    public void Title_SensorWaterLeak_ShowsWaterLeakMessage()
    {
        var ev = new SensorWaterLeakEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "leak");

        Assert.That(Title(ev, "Basement"), Is.EqualTo("Water leak detected (Basement)"));
    }

    [Test]
    public void Title_SensorBatteryLow_ShowsBatteryPercentage()
    {
        var ev = new SensorBatteryLowEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, 8.0);

        Assert.That(Title(ev, "Door Sensor"), Is.EqualTo("Low battery: 8% (Door Sensor)"));
    }

    [Test]
    public void Title_SensorExtremeValues_FormatsTypeStatusAndValueWithoutAssumingDecimalSeparator()
    {
        var ev = new SensorExtremeValuesEvent("id", 0, null, "dev", ProtectEventUpdateType.Add, "temperature", 38.5, "high");

        var title = Title(ev, "Attic Sensor");

        Assert.That(title.StartsWith("Temperature high:"), Is.True, $"Title was: {title}");
        Assert.That(title.EndsWith("(Attic Sensor)"), Is.True, $"Title was: {title}");
        Assert.That(title, Does.Contain("38"));
    }

    [Test]
    public void Title_UnknownEvent_ShowsEventType()
    {
        var ev = new UnknownEvent("id", "customEvent", 0, null, "dev", ProtectEventUpdateType.Add);

        Assert.That(Title(ev, "Camera"), Is.EqualTo("Event: customEvent (Camera)"));
    }

    [TestCase(new[] { "person" }, "Person")]
    [TestCase(new[] { "person", "vehicle" }, "Person, Vehicle")]
    [TestCase(new[] { "", "car" }, "Car")]
    [TestCase(new string[] { }, "")]
    public void FormatSmartTypes_CapitalizesAndJoinsNonEmptyTypes(string[] types, string expected)
    {
        Assert.That(ProtectEventDescriber.FormatSmartTypes(types), Is.EqualTo(expected));
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("door", "Door")]
    [TestCase("Window", "Window")]
    [TestCase("a", "A")]
    public void Capitalize_UppercasesFirstCharacterOnly(string? input, string expected)
    {
        Assert.That(ProtectEventDescriber.Capitalize(input), Is.EqualTo(expected));
    }
}
