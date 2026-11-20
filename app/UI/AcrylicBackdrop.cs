using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    internal enum AcrylicMode
    {
        Solid,
        Frame,
        FullWindow
    }

    internal sealed class AcrylicBackdrop : IDisposable
    {
        public const string Preference = "acrylic_backdrop";
        private const int SystemBackdropType = 38;
        private const int RedirectionBitmapAlpha = 39;
        private const int NoBackdrop = 1;
        private const int TransientWindowBackdrop = 3;
        private const uint Blackness = 0x00000042;
        private LiveAcrylicBackdrop? liveBackdrop;

        public bool IsLive => liveBackdrop is not null;

        public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);
        public static bool SupportsFullWindow => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26100);

        public static bool IsRequested => AppConfig.Get(Preference, 1) == 1;

        public static bool IsAvailable => IsSupported && !SystemInformation.HighContrast &&
            !Equals(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency", 1), 0);

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PatBlt(nint hdc, int x, int y, int width, int height, uint operation);

        public static int SetWindowAttribute(nint hwnd, int attribute, int value) =>
            DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));

        public AcrylicMode Apply(nint hwnd, bool enabled, Padding inset)
        {
            if (!IsSupported) return AcrylicMode.Solid;

            bool supportsAlpha = SupportsFullWindow;
            if (enabled && supportsAlpha && SetWindowAttribute(hwnd, RedirectionBitmapAlpha, 1) >= 0)
            {
                if (IsLive) return AcrylicMode.FullWindow;

                var client = new Margins();
                if (SetWindowAttribute(hwnd, SystemBackdropType, NoBackdrop) >= 0 &&
                    DwmExtendFrameIntoClientArea(hwnd, ref client) >= 0)
                {
                    liveBackdrop = LiveAcrylicBackdrop.TryCreate(hwnd);
                    if (IsLive) return AcrylicMode.FullWindow;
                }

                var full = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                if (SetWindowAttribute(hwnd, SystemBackdropType, TransientWindowBackdrop) >= 0 &&
                    DwmExtendFrameIntoClientArea(hwnd, ref full) >= 0) return AcrylicMode.FullWindow;
            }

            Dispose();
            if (supportsAlpha) SetWindowAttribute(hwnd, RedirectionBitmapAlpha, 0);
            if (enabled && SetWindowAttribute(hwnd, SystemBackdropType, TransientWindowBackdrop) >= 0)
            {
                var glass = new Margins { Left = inset.Left, Right = inset.Right, Top = inset.Top, Bottom = inset.Bottom };
                if (DwmExtendFrameIntoClientArea(hwnd, ref glass) >= 0) return AcrylicMode.Frame;
            }

            SetWindowAttribute(hwnd, SystemBackdropType, NoBackdrop);
            var solid = new Margins();
            DwmExtendFrameIntoClientArea(hwnd, ref solid);
            return AcrylicMode.Solid;
        }

        public void UpdateWindow(nint hwnd) => liveBackdrop?.UpdateWindow(hwnd);

        public void Dispose()
        {
            liveBackdrop?.Dispose();
            liveBackdrop = null;
        }

        public static void Clear(Graphics graphics, Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // The extended DWM frame shows through pixels cleared to zero, including their alpha channel.
            nint hdc = graphics.GetHdc();
            try
            {
                PatBlt(hdc, bounds.X, bounds.Y, bounds.Width, bounds.Height, Blackness);
            }
            finally { graphics.ReleaseHdc(hdc); }
        }
    }
}
