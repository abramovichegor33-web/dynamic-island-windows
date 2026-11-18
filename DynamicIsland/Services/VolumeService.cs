using System.Runtime.InteropServices;

namespace DynamicIsland.Services;

public class VolumeService
{
    private static readonly Lazy<VolumeService> _instance = new(() => new VolumeService());
    public static VolumeService Instance => _instance.Value;

    public float CurrentVolume { get; private set; } = 0.5f;
    public bool IsMuted { get; private set; } = false;

    public event Action<float, bool>? VolumeChanged;

    // Pin references to prevent GC collection while unmanaged COM callbacks are active
    private IMMDevice? _device;
    private IAudioEndpointVolume? _endpoint;
    private AudioEndpointVolumeCallback? _callback;

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

                _endpoint.GetMasterVolumeLevelScalar(out float vol);
                _endpoint.GetMute(out bool mute);
                CurrentVolume = vol;
                IsMuted = mute;

                _callback = new AudioEndpointVolumeCallback(this);
                _endpoint.RegisterControlChangeNotify(_callback);
            }
        }
        catch
        {
            // fallback if audio device unavailable
        }
    }

    internal void NotifyVolumeChanged(float volume, bool muted)
    {
        CurrentVolume = volume;
        IsMuted = muted;
        VolumeChanged?.Invoke(volume, muted);
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
        [PreserveSig]
        int RegisterControlChangeNotify(IAudioEndpointVolumeCallback pNotify);
        [PreserveSig]
        int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback pNotify);
        int NotImpl1();
        int NotImpl2();
        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int NotImpl3();
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int NotImpl4();
        int NotImpl5();
        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    [Guid("657804FA-D6AD-4496-8A60-352752D5004F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolumeCallback
    {
        [PreserveSig]
        int OnNotify(IntPtr pNotify);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AUDIO_VOLUME_NOTIFICATION_DATA
    {
        public Guid guidEventContext;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bMuted;
        public float fMasterVolume;
        public uint nChannels;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public float[] afChannelVolumes;
    }

    private class AudioEndpointVolumeCallback : IAudioEndpointVolumeCallback
    {
        private readonly VolumeService _parent;

        public AudioEndpointVolumeCallback(VolumeService parent)
        {
            _parent = parent;
        }

        public int OnNotify(IntPtr pNotify)
        {
            try
            {
                if (pNotify == IntPtr.Zero) return 0;
                var data = Marshal.PtrToStructure<AUDIO_VOLUME_NOTIFICATION_DATA>(pNotify);
                _parent.NotifyVolumeChanged(data.fMasterVolume, data.bMuted);
            }
            catch { }
            return 0;
        }
    }
}
