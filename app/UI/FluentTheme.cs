using System.Runtime.InteropServices;

namespace GHelper.UI
{
    internal static class FluentTheme
    {
        public static Color Canvas { get; private set; } = SystemColors.Control;
        public static Color Card { get; private set; } = SystemColors.Window;
        public static Color ControlFill { get; private set; } = SystemColors.Window;
        public static Color Stroke { get; private set; } = SystemColors.ControlDark;
        public static Color CardStroke { get; private set; } = SystemColors.ControlDark;
        public static Color Text { get; private set; } = SystemColors.WindowText;
        public static Color SecondaryText { get; private set; } = SystemColors.GrayText;
        public static Color DisabledText { get; private set; } = SystemColors.GrayText;
        public static Color Accent { get; private set; } = SystemColors.Highlight;
        public static bool HighContrast { get; private set; }

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetColorizationColor(out uint colorization, out bool opaqueBlend);

        public static void Update(bool dark, bool acrylic = false)
        {
            HighContrast = SystemInformation.HighContrast;
            if (HighContrast)
            {
                Canvas = Card = ControlFill = SystemColors.Window;
                Stroke = CardStroke = Text = SecondaryText = SystemColors.WindowText;
                DisabledText = SystemColors.GrayText;
                Accent = SystemColors.Highlight;
                return;
            }

            Canvas = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
            Card = dark ? Color.FromArgb(39, 39, 39) : Color.FromArgb(251, 251, 251);
            ControlFill = dark ? Color.FromArgb(50, 50, 50) : Color.White;
            Stroke = dark ? Color.FromArgb(72, 72, 72) : Color.FromArgb(216, 216, 216);
            CardStroke = dark ? Color.FromArgb(55, 55, 55) : Color.FromArgb(227, 227, 227);
            Text = dark ? Color.FromArgb(249, 249, 249) : Color.FromArgb(27, 27, 27);
            SecondaryText = dark ? Color.FromArgb(195, 195, 195) : Color.FromArgb(88, 88, 88);
            DisabledText = dark ? Color.FromArgb(148, 148, 148) : Color.FromArgb(114, 114, 114);

            Color systemAccent = DwmGetColorizationColor(out uint colorization, out _) == 0
                ? Color.FromArgb(unchecked((int)(colorization | 0xff000000)))
                : SystemColors.Highlight;
            Accent = ReadableAccent(systemAccent, ControlFill, dark);
            if (acrylic && dark)
            {
                Canvas = Blend(Canvas, Accent, 0.035f);
                Card = Blend(Blend(Card, Color.White, 0.045f), Accent, 0.055f);
                ControlFill = Blend(Blend(ControlFill, Color.White, 0.035f), Accent, 0.055f);
                CardStroke = Blend(CardStroke, Accent, 0.16f);
                Stroke = Blend(Stroke, Accent, 0.10f);
            }
        }

        private static Color ReadableAccent(Color accent, Color background, bool dark)
        {
            Color target = dark ? Color.White : Color.Black;
            for (int amount = 0; amount <= 10; amount++)
            {
                Color candidate = Blend(accent, target, amount / 10f);
                if (Contrast(candidate, background) >= 4.5) return candidate;
            }
            return target;
        }

        private static double Contrast(Color left, Color right)
        {
            double a = Luminance(left) + 0.05;
            double b = Luminance(right) + 0.05;
            return Math.Max(a, b) / Math.Min(a, b);
        }

        private static double Luminance(Color color)
        {
            static double Linear(byte channel)
            {
                double value = channel / 255.0;
                return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }

        public static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));

        public static int Scale(Control control, int dips) => (int)Math.Round(dips * control.DeviceDpi / 96f);
    }

    internal sealed class FluentCaption : Label
    {
    }
}
