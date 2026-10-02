using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Surveil.Domain.Cameras;

namespace Surveil.Application.Ports;

public sealed class NoOpCameraProvider : ICameraProvider
{
    public Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Camera>>([]);

    public Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RtspsStream>>([]);

    public Task<IReadOnlyList<RtspsStream>> CreateRtspsStreamsAsync(string cameraId, CancellationToken ct = default) =>
        throw new NotSupportedException("No camera provider is configured.");
}
