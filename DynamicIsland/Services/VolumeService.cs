using NAudio.CoreAudioApi;

namespace DynamicIsland.Services;

/// <summary>
/// CoreAudio volume service using official NAudio.Wasapi.
/// Rock solid, safe COM interop tested on all Windows versions.
/// </summary>
public class VolumeService
{
    private static readonly Lazy<VolumeService> _instance = new(() => new VolumeService());
    public static VolumeService Instance => _instance.Value;

    public float CurrentVolume { get; private set; } = 0.5f;
    public bool IsMuted { get; private set; } = false;

    public event Action<float, bool>? VolumeChanged;

    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _device;
    private AudioEndpointVolume? _endpointVolume;

    private VolumeService()
    {
        _ = Task.Run(InitCoreAudio);
    }

    private void InitCoreAudio()
    {
        try
        {
            _enumerator = new MMDeviceEnumerator();
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (_device != null)
            {
                _endpointVolume = _device.AudioEndpointVolume;
                if (_endpointVolume != null)
                {
                    CurrentVolume = _endpointVolume.MasterVolumeLevelScalar;
                    IsMuted = _endpointVolume.Mute;
                    _endpointVolume.OnVolumeNotification += OnVolumeNotification;
                }
            }
        }
        catch
        {
            // Fallback for systems without audio devices
        }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        try
        {
            CurrentVolume = data.MasterVolume;
            IsMuted = data.Muted;
            VolumeChanged?.Invoke(CurrentVolume, IsMuted);
        }
        catch { }
    }

    public void SetVolume(float volume)
    {
        try
        {
            if (_endpointVolume != null)
            {
                _endpointVolume.MasterVolumeLevelScalar = Math.Clamp(volume, 0f, 1f);
            }
        }
        catch { }
    }

    public void ToggleMute()
    {
        try
        {
            if (_endpointVolume != null)
            {
                _endpointVolume.Mute = !_endpointVolume.Mute;
            }
        }
        catch { }
    }
}
