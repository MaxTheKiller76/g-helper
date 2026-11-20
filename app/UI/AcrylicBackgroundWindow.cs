using System.Runtime.InteropServices;

namespace GHelper.UI
{
    // A composition-only window immediately behind the WinForms client area.
    internal sealed class AcrylicBackgroundWindow : NativeWindow, IDisposable
    {
        private const int StylePopup = unchecked((int)0x80000000);
        private const int StyleDisabled = 0x08000000;
        private const int ExStyleToolWindow = 0x00000080;
        private const int ExStyleNoActivate = 0x08000000;
        private const int NonClientHitTest = 0x0084;
        private const int HitTransparent = -1;
        private const int WindowCornerPreference = 33;
        private const int RoundCorners = 2;
        private const int UseHostBackdropBrush = 17;
        private const uint NoActivate = 0x0010;
        private const uint ShowWindowFlag = 0x0040;
        private const uint HideWindowFlag = 0x0080;
        private const uint NoOwnerZOrder = 0x0200;
        private const uint PreserveBoundsAndZOrder = 0x0007;

        private bool moving;

        public AcrylicBackgroundWindow()
        {
            CreateHandle(new CreateParams { Style = StylePopup | StyleDisabled, ExStyle = ExStyleToolWindow | ExStyleNoActivate });
            AcrylicBackdrop.SetWindowAttribute(Handle, WindowCornerPreference, RoundCorners);
        }

        public bool EnableBackdrop() => AcrylicBackdrop.SetWindowAttribute(Handle, UseHostBackdropBrush, 1) >= 0;

        public bool Follow(nint foreground)
        {
            if (moving) return true;
            moving = true;
            try
            {
                if (!IsWindowVisible(foreground) || IsIconic(foreground))
                {
                    if (IsWindowVisible(Handle))
                        SetWindowPos(Handle, 0, 0, 0, 0, 0, PreserveBoundsAndZOrder | NoActivate | HideWindowFlag);
                    return true;
                }

                if (!GetClientRect(foreground, out var bounds)) return false;
                var origin = new ScreenPoint();
                if (!ClientToScreen(foreground, ref origin)) return false;
                return SetWindowPos(Handle, foreground, origin.X, origin.Y, bounds.Right, bounds.Bottom,
                    NoActivate | ShowWindowFlag | NoOwnerZOrder);
            }
            finally { moving = false; }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NonClientHitTest) message.Result = HitTransparent;
            else base.WndProc(ref message);
        }

        public void Dispose()
        {
            if (Handle != 0) DestroyHandle();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ClientBounds { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct ScreenPoint { public int X, Y; }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(nint window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(nint window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(nint window, out ClientBounds bounds);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(nint window, ref ScreenPoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    }
}
