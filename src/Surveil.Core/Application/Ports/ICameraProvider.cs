using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Domain.Cameras;

namespace Surveil.Application.Ports;

public interface ICameraProvider
{
    Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default);
    Task<IReadOnlyList<RtspsStream>> CreateRtspsStreamsAsync(string cameraId, CancellationToken ct = default);
}
