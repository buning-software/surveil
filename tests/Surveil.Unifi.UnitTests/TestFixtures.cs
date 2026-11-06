using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Surveil.Unifi.WebSocket;

namespace Surveil.Unifi.UnitTests;

internal static class TestFixtures
{
    internal static EventNotificationSettings AllEventsEnabled() => new()
    {
        Motion = true, SmartDetectZone = true, SmartDetectLine = true,
        SmartDetectLoiterZone = true, SmartAudioDetect = true, Ring = true,
        LightMotion = true, SensorMotion = true, SensorTamper = true,
        SensorSmokeTest = true, SensorAlarm = true, SensorOpened = true,
        SensorClosed = true, SensorWaterLeak = true, SensorBatteryLow = true,
        SensorExtremeValues = true
    };

    internal static IOptions<UnifiProtectOptions> ProtectOptions(
        string baseUrl = "https://host", string apiKey = "key") =>
        Options.Create(new UnifiProtectOptions { BaseUrl = baseUrl, ApiKey = apiKey });

    internal static IWebSocketFactory WebSocketDelivering(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var receiveCount = 0;

        var connection = new Mock<IWebSocketConnection>();
        connection.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        connection.SetupGet(w => w.State)
                  .Returns(() => receiveCount == 0 ? WebSocketState.Open : WebSocketState.Closed);
        connection.Setup(w => w.ReceiveAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                  .Returns((Memory<byte> buffer, CancellationToken _) =>
                  {
                      bytes.CopyTo(buffer);
                      receiveCount++;
                      return new ValueTask<ValueWebSocketReceiveResult>(
                          new ValueWebSocketReceiveResult(bytes.Length, WebSocketMessageType.Text, true));
                  });

        var factory = new Mock<IWebSocketFactory>();
        factory.Setup(f => f.Create(It.IsAny<string>())).Returns(connection.Object);
        return factory.Object;
    }
}
