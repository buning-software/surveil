using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using LibVLCSharp.Shared;

namespace Surveil.Services;

[ExcludeFromCodeCoverage]
internal sealed class DefaultVlcPlayerFactory : IVlcPlayerFactory
{
    private static readonly Lazy<LibVLC> Shared = new(CreateShared);

    public static void WarmUp() => _ = Shared.Value;

    public IVlcPlayerHandle Create(string url, Action<string> onError) =>
        new VlcPlayerHandle(Shared.Value, url, onError);

    private static LibVLC CreateShared()
    {
        var architecture = Environment.Is64BitProcess ? "win-x64" : "win-x86";
        var vlcDirectory = Path.Combine(AppContext.BaseDirectory, "libvlc", architecture);
        LibVLCSharp.Shared.Core.Initialize(vlcDirectory);

        var libVlc = new LibVLC(enableDebugLogs: false);

        libVlc.Log += (_, args) =>
        {
            if (args.Level == LogLevel.Error)
                Debug.WriteLine($"[VLC Error] {args.FormattedLog}");
        };

        return libVlc;
    }
}
