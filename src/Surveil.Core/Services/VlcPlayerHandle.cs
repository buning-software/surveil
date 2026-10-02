using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace Surveil.Services;

[ExcludeFromCodeCoverage]
internal sealed class VlcPlayerHandle : IVlcPlayerHandle
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly Media _media;

    private readonly MediaPlayer.LibVLCVideoFormatCb _formatCb;
    private readonly MediaPlayer.LibVLCVideoCleanupCb _cleanupCb;
    private readonly MediaPlayer.LibVLCVideoLockCb _lockCb;
    private readonly MediaPlayer.LibVLCVideoUnlockCb _unlockCb;
    private readonly MediaPlayer.LibVLCVideoDisplayCb _displayCb;

    private byte[]? _pixelBuffer;
    private GCHandle _bufferHandle;
    private int _width;
    private int _height;

    public event EventHandler? Playing;
    public event EventHandler? EncounteredError;
    public event EventHandler? EndReached;
    public event EventHandler<VideoFrame>? FrameReady;

    public VlcPlayerHandle(LibVLC libVlc, string url, Action<string> onError)
    {
        libVlc.SetDialogHandlers(
            error: (title, text) =>
            {
                onError(text ?? title ?? "Unknown error");
                return Task.CompletedTask;
            },
            login: (dialog, _, _, _, _, _) =>
            {
                dialog.Dismiss();
                return Task.CompletedTask;
            },
            question: (dialog, _, _, _, _, _, _, _) =>
            {
                dialog.PostAction(1);
                return Task.CompletedTask;
            },
            displayProgress: (_, _, _, _, _, _, _) => Task.CompletedTask,
            updateProgress: (_, _, _) => Task.CompletedTask
        );

        _media = new Media(libVlc, new Uri(url));
        _media.AddOption(":rtsp-tcp");
        _media.AddOption(":network-caching=300");
        _media.AddOption(":clock-jitter=0");
        _media.AddOption(":clock-synchro=0");
        _media.AddOption(":no-audio");

        _mediaPlayer = new MediaPlayer(_media);

        _formatCb = OnVideoFormat;
        _cleanupCb = OnVideoCleanup;
        _lockCb = OnLock;
        _unlockCb = OnUnlock;
        _displayCb = OnDisplay;

        _mediaPlayer.SetVideoFormatCallbacks(_formatCb, _cleanupCb);
        _mediaPlayer.SetVideoCallbacks(_lockCb, _unlockCb, _displayCb);

        _mediaPlayer.Playing += (s, e) => Playing?.Invoke(s, e);
        _mediaPlayer.Buffering += (_, _) => { };
        _mediaPlayer.EncounteredError += (s, e) => EncounteredError?.Invoke(s, e);
        _mediaPlayer.EndReached += (s, e) => EndReached?.Invoke(s, e);
    }

    public void Play() => _mediaPlayer.Play();

    public void Stop() => _mediaPlayer.Stop();

    public void Dispose()
    {
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _media.Dispose();
        ReleaseBuffer();
    }

    private uint OnVideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height,
        ref uint pitches, ref uint lines)
    {
        _width = (int)width;
        _height = (int)height;

        Marshal.WriteByte(chroma, 0, (byte)'R');
        Marshal.WriteByte(chroma, 1, (byte)'V');
        Marshal.WriteByte(chroma, 2, (byte)'3');
        Marshal.WriteByte(chroma, 3, (byte)'2');

        pitches = width * 4;
        lines = height;

        ReleaseBuffer();
        _pixelBuffer = new byte[width * height * 4];
        _bufferHandle = GCHandle.Alloc(_pixelBuffer, GCHandleType.Pinned);

        return 1;
    }

    private void OnVideoCleanup(ref IntPtr opaque) => ReleaseBuffer();

    private IntPtr OnLock(IntPtr opaque, IntPtr planes)
    {
        if (_bufferHandle.IsAllocated)
            Marshal.WriteIntPtr(planes, _bufferHandle.AddrOfPinnedObject());
        return IntPtr.Zero;
    }

    private void OnUnlock(IntPtr opaque, IntPtr picture, IntPtr planes) { }

    private void OnDisplay(IntPtr opaque, IntPtr picture)
    {
        if (_pixelBuffer is null || _width == 0 || _height == 0) return;

        try
        {
            var dataLength = _width * _height * 4;
            var pixels = ArrayPool<byte>.Shared.Rent(dataLength);
            Buffer.BlockCopy(_pixelBuffer, 0, pixels, 0, dataLength);
            FrameReady?.Invoke(this, new VideoFrame(pixels, _width, _height, dataLength));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RtspVideoPlayer] Display error: {ex.Message}");
        }
    }

    private void ReleaseBuffer()
    {
        if (_bufferHandle.IsAllocated)
            _bufferHandle.Free();
        _pixelBuffer = null;
    }
}
