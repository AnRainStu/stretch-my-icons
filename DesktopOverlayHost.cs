using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using DrawingColor = System.Drawing.Color;
using DrawingIcon = System.Drawing.Icon;

namespace StretchMyIcons;

internal sealed class DesktopOverlayHost : NativeWindow, IDisposable
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "stretch-my-icons.log");
    private const int LvmGetItemCount = 0x1004;
    private const int LvmGetItemPosition = 0x1010;
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const int WsClipSiblings = 0x04000000;
    private const int WsClipChildren = 0x02000000;
    private const int WsExTransparent = 0x20;
    private const int WsExNoActivate = 0x08000000;
    private const int WmPaint = 0x000F;
    private const int WmEraseBkgnd = 0x0014;
    private const int WmNCHitTest = 0x0084;
    private const int HtTransparent = -1;
    private const int ProcessVmOperation = 0x0008;
    private const int ProcessVmRead = 0x0010;
    private const int ProcessVmWrite = 0x0020;

    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly List<OverlayTile> tiles = [];
    private IntPtr desktopView;
    private IntPtr iconList;
    private int columns = 1;
    private int rows = 1;

    public void Start()
    {
        Attach();
        refreshTimer.Tick += (_, _) => RefreshTiles();
        refreshTimer.Start();
    }

    public void Dispose()
    {
        refreshTimer.Stop();
        if (Handle != IntPtr.Zero) DestroyHandle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmEraseBkgnd)
        {
            m.Result = new IntPtr(1);
            return;
        }

        if (m.Msg == WmNCHitTest)
        {
            m.Result = new IntPtr(HtTransparent);
            return;
        }

        if (m.Msg == WmPaint)
        {
            PaintOverlay();
            m.Result = IntPtr.Zero;
            return;
        }

        base.WndProc(ref m);
    }

    private void Attach()
    {
        Log("Attach start");
        iconList = FindDesktopListView();
        Log($"iconList={iconList}");
        if (iconList == IntPtr.Zero) return;
        desktopView = GetParent(iconList);
        Log($"desktopView={desktopView}");
        if (desktopView == IntPtr.Zero) return;
        GetClientRect(desktopView, out var rect);
        Log($"rect={rect.Right}x{rect.Bottom}");

        if (Handle == IntPtr.Zero)
        {
            var cp = new CreateParams
            {
                Caption = string.Empty,
                ClassName = null,
                Style = WsChild | WsVisible | WsClipSiblings | WsClipChildren,
                ExStyle = WsExTransparent | WsExNoActivate,
                X = 0,
                Y = 0,
                Width = rect.Right,
                Height = rect.Bottom,
                Parent = desktopView
            };
            CreateHandle(cp);
            Log($"created handle={Handle}");
        }

        MoveWindow(Handle, 0, 0, rect.Right, rect.Bottom, true);
        SetWindowPos(Handle, iconList, 0, 0, rect.Right, rect.Bottom, 0x0010 | 0x0040);
        Log("Attach done");
        RefreshTiles();
    }

    private void RefreshTiles()
    {
        if (iconList == IntPtr.Zero || !IsWindow(iconList) || desktopView == IntPtr.Zero || !IsWindow(desktopView))
        {
            Log("Refresh reattach");
            Attach();
            return;
        }

        tiles.Clear();
        GetClientRect(desktopView, out var rect);
        MoveWindow(Handle, 0, 0, rect.Right, rect.Bottom, true);

        var count = (int)SendMessage(iconList, LvmGetItemCount, IntPtr.Zero, IntPtr.Zero);
        var cellWidth = 76;
        var cellHeight = 96;
        var positions = new List<POINT>();
        for (var index = 0; index < count; index++)
        {
            if (TryGetItemPosition(iconList, index, out var point)) positions.Add(point);
        }

        if (positions.Count > 1)
        {
            var xValues = positions.Select(point => point.X).Distinct().Order().ToArray();
            var yValues = positions.Select(point => point.Y).Distinct().Order().ToArray();
            if (xValues.Length > 1) cellWidth = Math.Clamp(xValues[1] - xValues[0], 48, 180);
            if (yValues.Length > 1) cellHeight = Math.Clamp(yValues[1] - yValues[0], 64, 180);
        }

        var desktopItems = EnumerateDesktopItems();
        for (var index = 0; index < positions.Count; index++)
        {
            var point = positions[index];
            var path = index < desktopItems.Count ? desktopItems[index] : string.Empty;
            var color = TryGetDominantColor(path);
            tiles.Add(new OverlayTile
            {
                Bounds = new Rectangle(Math.Max(0, point.X - 4), Math.Max(0, point.Y - 4), Math.Max(32, cellWidth * columns - 8), Math.Max(44, cellHeight * rows - 8)),
                BaseColor = color
            });
        }

        InvalidateRect(Handle, IntPtr.Zero, false);
        Log($"Refresh tiles={tiles.Count}");
    }

    private static void Log(string message)
    {
        File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
    }

    private void PaintOverlay()
    {
        BeginPaint(Handle, out var paint);
        try
        {
            using var graphics = Graphics.FromHwnd(Handle);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.Clear(DrawingColor.Transparent);
            foreach (var tile in tiles)
            {
                var light = ChangeBrightness(tile.BaseColor, 1.22);
                var dark = ChangeBrightness(tile.BaseColor, 0.68);
                using var brush = new LinearGradientBrush(tile.Bounds, light, dark, 135f);
                using var border = new Pen(DrawingColor.FromArgb(46, 255, 255, 255), 1f);
                using var path = CreateRoundRect(tile.Bounds, 10);
                graphics.FillPath(brush, path);
                graphics.DrawPath(border, path);
            }
        }
        finally
        {
            EndPaint(Handle, ref paint);
        }
    }

    private static GraphicsPath CreateRoundRect(Rectangle rect, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static List<string> EnumerateDesktopItems()
    {
        var paths = new List<string>();
        foreach (var directory in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        })
        {
            if (!Directory.Exists(directory)) continue;
            paths.AddRange(Directory.EnumerateFileSystemEntries(directory));
        }
        return paths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static DrawingColor TryGetDominantColor(string path)
    {
        try
        {
            using var icon = DrawingIcon.ExtractAssociatedIcon(path);
            using var bitmap = icon?.ToBitmap();
            if (bitmap is null) return DrawingColor.FromArgb(55, 61, 72);
            var buckets = new Dictionary<int, double>();
            for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A < 80) continue;
                var key = (pixel.R / 32 << 16) | (pixel.G / 32 << 8) | (pixel.B / 32);
                buckets[key] = buckets.GetValueOrDefault(key) + 1 + pixel.GetSaturation() * 3;
            }
            if (buckets.Count == 0) return DrawingColor.FromArgb(55, 61, 72);
            var winner = buckets.MaxBy(pair => pair.Value).Key;
            return DrawingColor.FromArgb((byte)(((winner >> 16) & 7) * 32 + 16), (byte)(((winner >> 8) & 7) * 32 + 16), (byte)((winner & 7) * 32 + 16));
        }
        catch { return DrawingColor.FromArgb(55, 61, 72); }
    }

    private static DrawingColor ChangeBrightness(DrawingColor color, double factor) =>
        DrawingColor.FromArgb((byte)Math.Clamp(color.R * factor, 0, 255), (byte)Math.Clamp(color.G * factor, 0, 255), (byte)Math.Clamp(color.B * factor, 0, 255));

    private static bool TryGetItemPosition(IntPtr list, int index, out POINT point)
    {
        point = default;
        GetWindowThreadProcessId(list, out var processId);
        var process = OpenProcess(ProcessVmOperation | ProcessVmRead | ProcessVmWrite, false, processId);
        if (process == IntPtr.Zero) return false;
        var remote = VirtualAllocEx(process, IntPtr.Zero, (IntPtr)Marshal.SizeOf<POINT>(), 0x1000 | 0x2000, 0x04);
        if (remote == IntPtr.Zero) { CloseHandle(process); return false; }
        SendMessage(list, LvmGetItemPosition, (IntPtr)index, remote);
        var ok = ReadProcessMemory(process, remote, out point, Marshal.SizeOf<POINT>(), out _);
        VirtualFreeEx(process, remote, IntPtr.Zero, 0x8000);
        CloseHandle(process);
        return ok;
    }

    private static IntPtr FindDesktopListView()
    {
        var progman = FindWindow("Progman", "Program Manager");
        var view = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (view == IntPtr.Zero)
        {
            var worker = IntPtr.Zero;
            while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
            {
                view = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (view != IntPtr.Zero) break;
            }
        }
        return FindWindowEx(view, IntPtr.Zero, "SysListView32", null);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? className, string? title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string? className, string? title);
    [DllImport("user32.dll", EntryPoint = "GetParent")] private static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out RECT rect);
    [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr window, IntPtr rect, bool erase);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr window, out PAINTSTRUCT paint);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr window, ref PAINTSTRUCT paint);
    [DllImport("kernel32.dll")] private static extern IntPtr OpenProcess(int access, bool inherit, uint processId);
    [DllImport("kernel32.dll")] private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, IntPtr size, int allocationType, int protection);
    [DllImport("kernel32.dll")] private static extern bool ReadProcessMemory(IntPtr process, IntPtr address, out POINT buffer, int size, out IntPtr read);
    [DllImport("kernel32.dll")] private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, IntPtr size, int freeType);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct PAINTSTRUCT { public IntPtr hdc; public bool erase; public RECT rcPaint; public bool restore; public bool incUpdate; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] reserved; }

    private sealed class OverlayTile
    {
        public Rectangle Bounds { get; init; }
        public DrawingColor BaseColor { get; init; }
    }
}
