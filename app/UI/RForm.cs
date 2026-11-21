using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    public class RForm : Form
    {

        public static Color colorEco = Color.FromArgb(255, 6, 180, 138);
        public static Color colorStandard = Color.FromArgb(255, 58, 174, 239);
        public static Color colorTurbo = Color.FromArgb(255, 255, 32, 32);
        public static Color colorCustom = Color.FromArgb(255, 255, 128, 0);
        public static Color colorGray = Color.FromArgb(255, 168, 168, 168);


        public static Color buttonMain;
        public static Color buttonSecond;

        public static Color formBack;
        public static Color foreMain;
        public static Color borderMain;
        public static Color borderSecond;
        public static Color chartMain;
        public static Color chartGrid;

        public static bool flatTheme = false;

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#135")]
        private static extern int SetPreferredAppMode(int preferredAppMode);

        [DllImport("UXTheme.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(nint hWnd, string pszSubAppName, string? pszSubIdList);

        private const int TransitionsForcedDisabled = 3;
        private const int CloakWindow = 13;
        private const int UseImmersiveDarkMode = 20;
        private const int CaptionColor = 35;
        private const int CaptionTextColor = 36;
        private const int WindowPositionChanged = 0x0047;

        public bool darkTheme = false;
        private bool themeInitialized = false;
        private Color themeAccent;
        private bool contrastTheme;
        private bool acrylicRequested;
        private AcrylicMode acrylicMode;
        private readonly AcrylicBackdrop acrylicBackdrop = new();
        private AcrylicSurface? acrylicSurface;
        private bool preparingAcrylicReveal;
        internal bool HasAcrylicFrame => acrylicMode == AcrylicMode.Frame;
        protected virtual Padding AcrylicInsets => Padding;
        protected override CreateParams CreateParams
        {
            get
            {
                var parms = base.CreateParams;
                parms.Style &= ~0x02000000;  // Turn off WS_CLIPCHILDREN
                parms.ClassStyle &= ~0x00020000;
                return parms;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (themeInitialized) ApplyWindowTheme();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            acrylicBackdrop.Dispose();
            base.OnHandleDestroyed(e);
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!value || Visible || acrylicSurface is null || preparingAcrylicReveal || !IsHandleCreated)
            {
                base.SetVisibleCore(value);
                return;
            }

            nint window = Handle;
            if (AcrylicBackdrop.SetWindowAttribute(window, CloakWindow, 1) < 0)
            {
                base.SetVisibleCore(value);
                return;
            }

            // Let WinForms lay out and paint every child before DWM presents the first frame.
            preparingAcrylicReveal = true;
            try
            {
                base.SetVisibleCore(value);
                if (IsHandleCreated && Handle == window && Visible)
                {
                    AcrylicSurface.PaintWholeWindow(window);
                    acrylicBackdrop.UpdateWindow(window);
                }
            }
            finally
            {
                preparingAcrylicReveal = false;
                if (IsHandleCreated && Handle == window)
                    AcrylicBackdrop.SetWindowAttribute(window, CloakWindow, 0);
            }
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (message.Msg == WindowPositionChanged && IsHandleCreated && !preparingAcrylicReveal)
                acrylicBackdrop?.UpdateWindow(message.HWnd);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (!HasAcrylicFrame) return;

            var area = ClientRectangle;
            var insets = AcrylicInsets;
            AcrylicBackdrop.Clear(e.Graphics, Rectangle.Intersect(e.ClipRectangle,
                new Rectangle(area.Left, area.Top, area.Width, insets.Top)));
            AcrylicBackdrop.Clear(e.Graphics, Rectangle.Intersect(e.ClipRectangle,
                new Rectangle(area.Left, area.Bottom - insets.Bottom, area.Width, insets.Bottom)));
            AcrylicBackdrop.Clear(e.Graphics, Rectangle.Intersect(e.ClipRectangle,
                new Rectangle(area.Left, area.Top, insets.Left, area.Height)));
            AcrylicBackdrop.Clear(e.Graphics, Rectangle.Intersect(e.ClipRectangle,
                new Rectangle(area.Right - insets.Right, area.Top, insets.Right, area.Height)));
        }

        private void ApplyWindowTheme()
        {
            int immersiveDarkMode = darkTheme && !contrastTheme ? 1 : 0;
            AcrylicBackdrop.SetWindowAttribute(Handle, UseImmersiveDarkMode, immersiveDarkMode);
            acrylicMode = acrylicBackdrop.Apply(Handle, acrylicRequested, AcrylicInsets);
            // DWM's window fade briefly exposes the bright backdrop through the translucent client area.
            AcrylicBackdrop.SetWindowAttribute(Handle, TransitionsForcedDisabled, acrylicBackdrop.IsLive ? 1 : 0);
            if (acrylicMode == AcrylicMode.FullWindow)
            {
                acrylicSurface ??= new AcrylicSurface(this);
                acrylicSurface.HasLiveBackdrop = acrylicBackdrop.IsLive;
            }
            else
            {
                acrylicSurface?.Dispose();
                acrylicSurface = null;
            }

            bool defaultCaption = contrastTheme || acrylicMode != AcrylicMode.Solid;
            int caption = defaultCaption && !acrylicBackdrop.IsLive ? -1 : ColorTranslator.ToWin32(formBack);
            int captionText = defaultCaption ? -1 : ColorTranslator.ToWin32(foreMain);
            AcrylicBackdrop.SetWindowAttribute(Handle, CaptionColor, caption);
            AcrylicBackdrop.SetWindowAttribute(Handle, CaptionTextColor, captionText);
            SetPreferredAppMode(immersiveDarkMode);
            SetWindowTheme(Handle, immersiveDarkMode == 1 ? "DarkMode_Explorer" : "Explorer", null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                acrylicSurface?.Dispose();
                acrylicSurface = null;
                acrylicBackdrop.Dispose();
            }
            base.Dispose(disposing);
        }

        public static void InitColors(bool darkTheme, bool acrylic = false)
        {
            flatTheme = AppConfig.GetString("theme")?.ToLower() == "flat";
            FluentTheme.Update(darkTheme, acrylic);
            buttonMain = FluentTheme.ControlFill;
            buttonSecond = FluentTheme.Card;
            formBack = FluentTheme.Canvas;
            foreMain = FluentTheme.Text;
            borderMain = FluentTheme.Stroke;
            borderSecond = FluentTheme.CardStroke;
            chartMain = FluentTheme.Card;
            chartGrid = FluentTheme.Blend(chartMain, foreMain, 0.19f);
        }

        private static bool IsDarkTheme()
        {
            if (SystemInformation.HighContrast) return SystemColors.Window.GetBrightness() < 0.5f;

            string? uiMode = AppConfig.GetString("ui_mode");

            if (uiMode is not null && uiMode.ToLower() == "dark")
            {
                return true;
            }

            if (uiMode is not null && uiMode.ToLower() == "light")
            {
                return false;
            }

            if (uiMode is not null && uiMode.ToLower() == "windows")
            {
                return CheckSystemDarkModeStatus();
            }

            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var registryValueObject = key?.GetValue("AppsUseLightTheme");

            if (registryValueObject == null) return false;
            return (int)registryValueObject <= 0;
        }

        public bool InitTheme(bool setDPI = false)
        {
            bool newDarkTheme = IsDarkTheme();
            bool darkChanged = darkTheme != newDarkTheme;
            bool firstInit = !themeInitialized;
            darkTheme = newDarkTheme;

            bool enabled = AcrylicBackdrop.IsRequested && AcrylicBackdrop.IsAvailable;
            bool requested = enabled && TransparencyKey.IsEmpty && Opacity == 1;
            InitColors(darkTheme, enabled && AcrylicBackdrop.SupportsFullWindow);
            bool changed = darkChanged || themeAccent != FluentTheme.Accent || contrastTheme != FluentTheme.HighContrast ||
                acrylicRequested != requested;
            acrylicRequested = requested;
            themeAccent = FluentTheme.Accent;
            contrastTheme = FluentTheme.HighContrast;

            if (setDPI)
                ControlHelper.Resize(this);

            if (changed || firstInit)
            {
                ApplyWindowTheme();
                ControlHelper.Adjust(this, darkChanged);
                themeInitialized = true;
                Invalidate(true);
            }


            return changed;

        }

    }
}
