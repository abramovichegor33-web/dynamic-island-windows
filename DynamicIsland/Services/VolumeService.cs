using System.Runtime.InteropServices;

namespace DynamicIsland.Services;

/// <summary>
/// Crash-free CoreAudio volume service using safe timer polling.
/// Avoids unmanaged COM callback memory violations.
/// </summary>
public class VolumeService
{
    private static readonly Lazy<VolumeService> _instance = new(() => new VolumeService());
    public static VolumeService Instance => _instance.Value;

    public float CurrentVolume { get; private set; } = 0.5f;
    public bool IsMuted { get; private set; } = false;

    public event Action<float, bool>? VolumeChanged;

    private IMMDevice? _device;
    private IAudioEndpointVolume? _endpoint;
    private System.Threading.Timer? _pollTimer;

    private VolumeService()
    {
        _ = Task.Run(InitCoreAudio);
    }

    private void InitCoreAudio()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            enumerator.GetDefaultAudioEndpoint(0 /* eRender */, 1 /* eMultimedia */, out _device);
            if (_device != null)
            {
                var iid = typeof(IAudioEndpointVolume).GUID;
                _device.Activate(ref iid, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out var audioEndpointObj);
                _endpoint = (IAudioEndpointVolume)audioEndpointObj;

                CheckVolume();

                // Poll volume state every 300ms safely without unmanaged callback crashes
                _pollTimer = new System.Threading.Timer(_ => CheckVolume(), null, 500, 300);
            }
        }
        catch
        {
            // fallback
        }
    }

    private void CheckVolume()
    {
        if (_endpoint == null) return;
        try
        {
            _endpoint.GetMasterVolumeLevelScalar(out float vol);
            _endpoint.GetMute(out bool mute);

            if (Math.Abs(vol - CurrentVolume) > 0.01f || mute != IsMuted)
            {
                CurrentVolume = vol;
                IsMuted = mute;
                VolumeChanged?.Invoke(vol, mute);
            }
        }
        catch { }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();
        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int NotImpl0();
        int NotImpl1();
        int NotImpl2();
        int NotImpl3();
        int NotImpl4();
        int NotImpl5();
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int NotImpl7();
        int NotImpl8();
        int NotImpl9();
        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }
}
