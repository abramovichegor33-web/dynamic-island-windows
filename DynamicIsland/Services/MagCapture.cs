using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace DynamicIsland.Services;

/// <summary>
/// A picture of the screen WITHOUT one window (the island), taken through the Windows Magnification API:
/// its filter list leaves chosen windows out of what it draws. Unlike hiding the window from all capture,
/// the island stays visible to screenshots, recordings and screen sharing — and the glass still sees
/// what is behind it. The magnifier lives on a thread of its own, so a grab never holds the island's UI
/// thread. Not every Windows build delivers the picture: Works says whether this one does.
/// </summary>
public static class MagCapture
{
    private static readonly Native.MagScalingCallback Callback = OnImage;   // kept alive for as long as the process runs
    private static readonly object StartLock = new();
    private static volatile Dispatcher? _thread;
    private static volatile bool _failed;
    private static volatile int _works = -1;

    // the fields below are only touched on the magnifier's thread
    private static IntPtr _host, _mag, _excluded;
    private static int _magW, _magH;
    private static bool _initialized;
    private static byte[]? _full;
    private static int _fullW, _fullH, _fullStride, _wantW, _wantH;
    private static bool _got;
    private static string _lastProblem = "";

    // ---- crash guard ----
    // Inside Windows the Magnification API can take the whole process down — no exception, on any thread — and
    // on some PCs it did so on the first grabs: Atoll 1.8.0 then never stayed open with light graphics off. A
    // marker is written before the first grab and removed after a few good ones or on a normal exit. Still there
    // at the next start means the last run died in here: the magnifier stays off (solid glass while the island
    // is visible to capture) until the user asks to try again on the Check page.
    private static string GuardDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DynamicIsland");
    private static string PendingFile => Path.Combine(GuardDir, "magnifier.pending");
    private static string DisabledFile => Path.Combine(GuardDir, "magnifier.disabled");
    private const int GoodGrabsToTrust = 20;
    private static int _goodGrabs;
    private static bool _guardChecked;

    /// <summary>Test and picture runs (killed at will) leave no marker behind.</summary>
    public static bool UseGuard { get; set; } = true;

    /// <summary>Switched off because an earlier run died inside the magnifier.</summary>
    public static bool CrashedBefore { get; private set; }

    /// <summary>UI thread, before the first grab.</summary>
    private static bool GuardAllows()
    {
        if (_guardChecked || !UseGuard) return !_failed;
        _guardChecked = true;
        try
        {
            if (File.Exists(PendingFile))
            {
                File.WriteAllText(DisabledFile, "Atoll died inside the Magnification API; the glass uses no magnifier until this file is gone.");
                File.Delete(PendingFile);
                Log.Write("glass (magnifier): the last run died inside it — switched off");
            }
            if (File.Exists(DisabledFile))
            {
                CrashedBefore = true;
                _failed = true;
                _works = 0;
                return false;
            }
            Directory.CreateDirectory(GuardDir);
            File.WriteAllText(PendingFile, DateTime.UtcNow.ToString("o"));
        }
        catch (Exception e) { Log.Write("glass (magnifier): guard: " + e.Message); }   // no guard is better than no glass
        return true;
    }

    /// <summary>A normal exit, or enough good grabs: Windows copes with the magnifier here.</summary>
    public static void MarkClean()
    {
        if (!UseGuard) return;
        try { File.Delete(PendingFile); }
        catch (Exception e) { Log.Write("glass (magnifier): guard: " + e.Message); }
    }

    /// <summary>The Check page's "Try again": the magnifier is allowed once more (guarded the same way).</summary>
    public static void TryAgain()
    {
        try { File.Delete(DisabledFile); }
        catch (Exception e) { Log.Write("glass (magnifier): guard: " + e.Message); }
        CrashedBefore = false;
        _guardChecked = false;
        _failed = false;
        _works = -1;
        _lastProblem = "";
    }

    /// <summary>Tests: the size of the picture Windows handed over last.</summary>
    public static (int W, int H) LastSize => (_fullW, _fullH);

    /// <summary>null: not tried yet.</summary>
    public static bool? Works => _works < 0 ? null : _works == 1;

    /// <summary>
    /// The screen area r without the window "exclude", shrunk to (w, h) pixels, as BGRA bytes — or null where
    /// this Windows doesn't deliver the picture. The buffer is written on the magnifier's thread: don't touch
    /// it until the task is done.
    /// </summary>
    public static Task<byte[]?> CaptureAsync(IntPtr exclude, System.Drawing.Rectangle r, int w, int h, byte[]? buffer)
    {
        if (_failed || w < 1 || h < 1 || r.Width < 1 || r.Height < 1 || !GuardAllows()) return Task.FromResult<byte[]?>(null);
        var thread = Thread();
        if (thread == null) return Task.FromResult<byte[]?>(null);
        return thread.InvokeAsync(() => CaptureHere(exclude, r, w, h, buffer)).Task;
    }

    /// <summary>Tests: the same, waiting for the picture (at most two seconds).</summary>
    public static byte[]? Capture(IntPtr exclude, System.Drawing.Rectangle r, int w, int h, byte[]? buffer)
    {
        var t = CaptureAsync(exclude, r, w, h, buffer);
        return t.Wait(2000) ? t.Result : null;
    }

    private static Dispatcher? Thread()
    {
        if (_thread != null) return _thread;
        lock (StartLock)
        {
            if (_thread != null) return _thread;
            using var ready = new ManualResetEventSlim();
            var t = new System.Threading.Thread(() =>
            {
                _thread = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            }) { IsBackground = true, Name = "Atoll glass" };
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            ready.Wait();
            return _thread;
        }
    }

    private static void Fail(string why)
    {
        _failed = true;
        _works = 0;
        if (why != _lastProblem) Log.Write("glass (magnifier): " + why);
        _lastProblem = why;
    }

    private static bool OnImage(IntPtr hwnd, IntPtr src, Native.MAGIMAGEHEADER h, IntPtr dst, Native.MAGIMAGEHEADER dh, Native.RECT unclipped, Native.RECT clipped, IntPtr dirty)
    {
        try
        {
            // only the part that was asked for, row by row: the picture can be bigger than the grab
            int cw = Math.Min((int)h.Width, _wantW), ch = Math.Min((int)h.Height, _wantH), stride = cw * 4;
            if (cw < 1 || ch < 1) return true;
            if (_full == null || _full.Length < stride * ch) _full = new byte[stride * ch];
            for (int y = 0; y < ch; y++) Marshal.Copy(src + (int)h.Offset + y * (int)h.Stride, _full, y * stride, stride);
            _fullW = cw; _fullH = ch; _fullStride = stride;
            _got = true;
        }
        catch (Exception e)
        {
            if (_lastProblem != e.Message) Log.Write("glass (magnifier): picture: " + e.Message);
            _lastProblem = e.Message;
        }
        return true;
    }

    /// <summary>
    /// A magnifier window at least w × h. Not the size of all the screens: the picture Windows hands over is
    /// as big as the window, and copying the whole desktop every grab was the CPU load of 1.8.0. Resizing a
    /// magnifier window while Windows draws into it crashes inside Windows (seen in the field), so a bigger
    /// one is a new window — between grabs, on this thread — never a resize.
    /// </summary>
    private static bool Ensure(IntPtr exclude, int w, int h)
    {
        if (!_initialized)
        {
            if (!Native.MagInitialize()) { Fail("MagInitialize failed"); return false; }
            _initialized = true;
        }
        if (_mag != IntPtr.Zero && !Native.IsWindow(_mag)) { _mag = _host = IntPtr.Zero; }
        if (_mag == IntPtr.Zero || w > _magW || h > _magH)
        {
            if (_host != IntPtr.Zero) Native.DestroyWindow(_host);   // the magnifier is its child: it goes too
            _host = _mag = _excluded = IntPtr.Zero;
            // a little room to grow (the island opens wider than it folds), in steps of 64 pixels
            static int Room(int need, int was) => Math.Max(was, (need * 5 / 4 + 63) / 64 * 64);
            _magW = Room(w, _magW);
            _magH = Room(h, _magH);
            var inst = Native.GetModuleHandle(null);
            _host = Native.CreateWindowEx(Native.WS_EX_LAYERED, "Static", "AtollGlassHost", Native.WS_POPUP, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);
            if (_host == IntPtr.Zero) { Fail("no host window (" + Marshal.GetLastWin32Error() + ")"); return false; }
            _mag = Native.CreateWindowEx(0, "Magnifier", "AtollGlass", Native.WS_CHILD | Native.WS_VISIBLE, 0, 0, _magW, _magH, _host, IntPtr.Zero, inst, IntPtr.Zero);
            if (_mag == IntPtr.Zero) { Fail("no magnifier window (" + Marshal.GetLastWin32Error() + ")"); return false; }
            if (!Native.MagSetImageScalingCallback(_mag, Callback)) { Fail("MagSetImageScalingCallback failed"); return false; }
            Log.Write($"glass (magnifier): window {_magW}x{_magH}");
        }
        if (_excluded != exclude)
        {
            if (!Native.MagSetWindowFilterList(_mag, Native.MW_FILTERMODE_EXCLUDE, 1, [exclude])) { Fail("MagSetWindowFilterList failed"); return false; }
            _excluded = exclude;
        }
        return true;
    }

    private static byte[]? CaptureHere(IntPtr exclude, System.Drawing.Rectangle r, int w, int h, byte[]? buffer)
    {
        if (_failed) return null;
        try
        {
            var all = System.Windows.Forms.SystemInformation.VirtualScreen;
            if (r.Width > Math.Max(all.Width, 640) || r.Height > Math.Max(all.Height, 480)) return null;   // bigger than the screens: nothing sensible to grab
            if (!Ensure(exclude, r.Width, r.Height)) return null;
            _got = false;
            _wantW = r.Width; _wantH = r.Height;
            if (!Native.MagSetWindowSource(_mag, new Native.RECT { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom })) { Fail("MagSetWindowSource failed"); return null; }
            if (!_got || _full == null) { Fail("this Windows gives no picture"); return null; }
            _works = 1;
            if (++_goodGrabs == GoodGrabsToTrust) MarkClean();
            if (buffer == null || buffer.Length != w * h * 4) buffer = new byte[w * h * 4];
            // box-shrink the picture to w × h (it is blurred afterwards anyway)
            int srcW = _fullW, srcH = _fullH;
            double sx = (double)srcW / w, sy = (double)srcH / h;
            for (int y = 0; y < h; y++)
            {
                int y0 = Math.Min(srcH - 1, (int)(y * sy)), y1 = Math.Max(y0 + 1, Math.Min(srcH, (int)((y + 1) * sy)));
                for (int x = 0; x < w; x++)
                {
                    int x0 = Math.Min(srcW - 1, (int)(x * sx)), x1 = Math.Max(x0 + 1, Math.Min(srcW, (int)((x + 1) * sx)));
                    int b = 0, g = 0, rr = 0, n = 0;
                    for (int yy = y0; yy < y1; yy++)
                    {
                        int row = yy * _fullStride;
                        for (int xx = x0; xx < x1; xx++) { int i = row + xx * 4; b += _full[i]; g += _full[i + 1]; rr += _full[i + 2]; n++; }
                    }
                    int o = (y * w + x) * 4;
                    buffer[o] = (byte)(b / n); buffer[o + 1] = (byte)(g / n); buffer[o + 2] = (byte)(rr / n); buffer[o + 3] = 255;
                }
            }
            return buffer;
        }
        catch (Exception e) { Fail(e.GetType().Name + ": " + e.Message); return null; }
    }
}
