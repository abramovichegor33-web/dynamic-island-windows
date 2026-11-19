using System.Runtime.InteropServices;
using System.Windows.Threading;
using DynamicIsland.Services;
using Microsoft.Win32.SafeHandles;
using NAudio.CoreAudioApi;

namespace DynamicIsland;

/// <summary>
/// Volume and screen brightness: while either changes (keys, the mixer, a headset wheel), a small level
/// shows beside the island — the same little pill Caps Lock uses, so nothing on the island folds.
/// </summary>
public partial class MainWindow
{
    private readonly MMDeviceEnumerator _levelDevices = new();
    private MMDevice? _volumeDevice;
    private string _volumeDeviceId = "";
    private float _volumeWas = -1;
    private bool _muteWas;
    private string _volumeProblem = "";

    /// <summary>Every few seconds: follow the default output (headphones plugged in) and listen to its volume.</summary>
    private void WatchVolumeDevice()
    {
        if (!_settings.ShowLevels || _demoMedia != null) return;
        try
        {
            var d = _levelDevices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            // asked every few seconds: a COM object each time, released at once when it is the same device
            if (d.ID == _volumeDeviceId) { d.Dispose(); return; }
            if (_volumeDevice != null)
            {
                try { _volumeDevice.AudioEndpointVolume.OnVolumeNotification -= OnVolume; } catch { }
                _volumeDevice.Dispose();
            }
            _volumeDevice = d;
            _volumeDeviceId = d.ID;
            var v = d.AudioEndpointVolume;
            _volumeWas = v.MasterVolumeLevelScalar;   // a new device is not a volume change
            _muteWas = v.Mute;
            v.OnVolumeNotification += OnVolume;
            _volumeProblem = "";
        }
        catch (Exception e)
        {
            if (_volumeProblem != e.Message) Log.Write("levels: no default output device: " + e.Message);
            _volumeProblem = e.Message;
            _volumeDeviceId = "";
            _volumeDevice = null;
        }
    }

    /// <summary>Called by Windows on its own thread.</summary>
    private void OnVolume(AudioVolumeNotificationData data)
    {
        float vol = data.MasterVolume;
        bool mute = data.Muted;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (!_settings.ShowLevels) return;
            // Windows also reports changes of one channel's balance and repeats itself: only real changes count
            if (Math.Abs(vol - _volumeWas) < 0.004 && mute == _muteWas) return;
            _volumeWas = vol;
            _muteWas = mute;
            ShowLevel(mute || vol < 0.005 ? "\uE74F" : vol < 0.34 ? "\uE993" : vol < 0.67 ? "\uE994" : "\uE995",
                      mute ? L.T("Звук выключен") : L.T("Громкость"), mute ? 0 : vol, "volume");
        });
    }

    private void ShowLevel(string glyph, string title, double level, string kind) =>
        ShowKeyPill(glyph, title, $"{Math.Round(level * 100):0}%", null, level, kind);

    // ------------------------------------------------------------------ screen brightness (laptops)

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle device, uint code, IntPtr inBuf, uint inSize, byte[] outBuf, uint outSize, out uint returned, IntPtr overlapped);

    private const uint IoctlQueryBrightness = 0x00230498;   // IOCTL_VIDEO_QUERY_DISPLAY_BRIGHTNESS
    private SafeFileHandle? _lcd;
    private bool _noLcd;
    private int _brightnessWas = -1;
    private bool _brightnessBusy;

    /// <summary>Disabled to avoid monitor bus polling and game micro-stutters.</summary>
    private int ReadBrightness() => -1;

    /// <summary>
    /// Disabled to avoid display driver polling 4 times a second.
    /// </summary>
    private void WatchBrightness()
    {
    }
}
