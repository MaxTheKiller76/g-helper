using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    public class RComboBox : ComboBox
    {
        private const int ItemTextPadding = 6;
        public bool UseCustomTextPadding = true;

        public RComboBox()
        {
            DrawMode = UseCustomTextPadding ? DrawMode.OwnerDrawFixed : DrawMode.Normal;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var args = (HandledMouseEventArgs)e;
            if (!DroppedDown)
                args.Handled = true;
            else
                base.OnMouseWheel(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            CalibrateItemHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            CalibrateItemHeight();
        }

        public bool NativeHeight { get; set; }

        private void CalibrateItemHeight()
        {
            if (DrawMode != DrawMode.OwnerDrawFixed || NativeHeight) return;
            int chrome = PreferredHeight - ItemHeight;
            int target = (int)Math.Round(44 * (DeviceDpi / 192f));
            ItemHeight = Math.Max(1, target - chrome);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (!UseCustomTextPadding || e.Index < 0)
            {
                base.OnDrawItem(e);
                return;
            }

            bool selected = (e.State & (DrawItemState.Selected | DrawItemState.ComboBoxEdit)) == DrawItemState.Selected;
            Color bg = selected
                ? FluentTheme.HighContrast ? SystemColors.Highlight : FluentTheme.Blend(BackColor, FluentTheme.Accent, 0.16f)
                : BackColor;
            Color fg = !Enabled ? FluentTheme.DisabledText
                : selected && FluentTheme.HighContrast ? SystemColors.HighlightText : ForeColor;

            using (var bgBrush = new SolidBrush(bg))
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

            int pad = Math.Min(FluentTheme.Scale(this, ItemTextPadding), e.Bounds.Width / 4);
            var textRect = Rectangle.Inflate(e.Bounds, -pad, 0);
            TextFormatFlags alignment = RightToLeft == RightToLeft.Yes
                ? TextFormatFlags.RightToLeft | TextFormatFlags.Right : TextFormatFlags.Left;

            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), e.Font, textRect, fg,
                alignment | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }

        private Color borderColor = Color.Gray;
        [DefaultValue(typeof(Color), "Gray")]
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                if (borderColor != value)
                {
                    borderColor = value;
                    Invalidate();
                }
            }
        }


        private Color buttonColor = Color.FromArgb(255, 255, 255, 255);
        [DefaultValue(typeof(Color), "255, 255, 255")]
        public Color ButtonColor
        {
            get { return buttonColor; }
            set
            {
                if (buttonColor != value)
                {
                    buttonColor = value;
                    Invalidate();
                }
            }
        }

        private Color arrowColor = Color.Black;
        [DefaultValue(typeof(Color), "Black")]
        public Color ArrowColor
        {
            get { return arrowColor; }
            set
            {
                if (arrowColor != value)
                {
                    arrowColor = value;
                    Invalidate();
                }
            }
        }


        public static GraphicsPath RoundedRect(Rectangle bounds, int radiusL, int radiusR)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0) return path;

            int limit = Math.Min(bounds.Width, bounds.Height) / 2;
            radiusL = Math.Clamp(radiusL, 0, limit);
            radiusR = Math.Clamp(radiusR, 0, limit);
            int diameterL = radiusL * 2;
            int diameterR = radiusR * 2;
            path.AddLine(bounds.Left + radiusL, bounds.Top, bounds.Right - radiusR, bounds.Top);
            if (radiusR > 0) path.AddArc(bounds.Right - diameterR, bounds.Top, diameterR, diameterR, 270, 90);
            path.AddLine(bounds.Right, bounds.Top + radiusR, bounds.Right, bounds.Bottom - radiusR);
            if (radiusR > 0) path.AddArc(bounds.Right - diameterR, bounds.Bottom - diameterR, diameterR, diameterR, 0, 90);
            path.AddLine(bounds.Right - radiusR, bounds.Bottom, bounds.Left + radiusL, bounds.Bottom);
            if (radiusL > 0) path.AddArc(bounds.Left, bounds.Bottom - diameterL, diameterL, diameterL, 90, 90);
            path.AddLine(bounds.Left, bounds.Bottom - radiusL, bounds.Left, bounds.Top + radiusL);
            if (radiusL > 0) path.AddArc(bounds.Left, bounds.Top, diameterL, diameterL, 180, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle bounds, int cornerRadiusL = 5, int cornerRadiusR = 5)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            using (GraphicsPath path = RoundedRect(bounds, cornerRadiusL, cornerRadiusR))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadiusL = 5, int cornerRadiusR = 5)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            using (GraphicsPath path = RoundedRect(bounds, cornerRadiusL, cornerRadiusR))
            {
                graphics.FillPath(brush, path);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PAINT && DropDownStyle != ComboBoxStyle.Simple &&
                ClientSize.Width > SystemInformation.HorizontalScrollBarArrowWidth + 6 && ClientSize.Height > 6)
            {
                var clientRect = ClientRectangle;
                var dropDownButtonWidth = SystemInformation.HorizontalScrollBarArrowWidth;
                var outerBorder = new Rectangle(clientRect.Location,
                    new Size(clientRect.Width - 1, clientRect.Height - 1));
                var innerBorder = new Rectangle(outerBorder.X + 1, outerBorder.Y + 1,
                    outerBorder.Width - dropDownButtonWidth - 2, outerBorder.Height - 2);
                var innerInnerBorder = new Rectangle(innerBorder.X + 1, innerBorder.Y + 1,
                    innerBorder.Width - 2, innerBorder.Height - 2);
                var dropDownRect = new Rectangle(innerBorder.Right + 1, innerBorder.Y,
                    dropDownButtonWidth, innerBorder.Height + 1);
                if (RightToLeft == RightToLeft.Yes)
                {
                    innerBorder.X = clientRect.Width - innerBorder.Right;
                    innerInnerBorder.X = clientRect.Width - innerInnerBorder.Right;
                    dropDownRect.X = clientRect.Width - dropDownRect.Right;
                    dropDownRect.Width += 1;
                }
                var innerBorderColor = BackColor;
                var outerBorderColor = BorderColor;
                var buttonColor = ButtonColor;
                var middle = new Point(dropDownRect.Left + dropDownRect.Width / 2,
                    dropDownRect.Top + dropDownRect.Height / 2);
                int arrowHalfWidth = FluentTheme.Scale(this, 4);
                int arrowHalfHeight = FluentTheme.Scale(this, 2);
                Point[] arrow =
                [
                    new Point(middle.X - arrowHalfWidth, middle.Y - arrowHalfHeight),
                    new Point(middle.X, middle.Y + arrowHalfHeight),
                    new Point(middle.X + arrowHalfWidth, middle.Y - arrowHalfHeight)
                ];
                bool active = Focused || DroppedDown;
                int outerRadius = RForm.flatTheme ? 0 : FluentTheme.Scale(this, 4);
                var formBackColor = Parent is not null ? Parent.BackColor : BackColor;

                var ps = new PAINTSTRUCT();
                bool shoulEndPaint = false;
                nint dc;
                if (m.WParam == nint.Zero)
                {
                    dc = BeginPaint(Handle, ref ps);
                    m.WParam = dc;
                    shoulEndPaint = true;
                }
                else
                {
                    dc = m.WParam;
                }

                using (var g = Graphics.FromHdc(dc))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var b = new SolidBrush(formBackColor))
                        g.FillRectangle(b, clientRect);
                    using (var b = new SolidBrush(buttonColor))
                        FillRoundedRectangle(g, b, outerBorder, outerRadius, outerRadius);
                }

                var rgn = CreateRectRgn(innerInnerBorder.Left, innerInnerBorder.Top,
                    innerInnerBorder.Right, innerInnerBorder.Bottom);

                SelectClipRgn(dc, rgn);
                DefWndProc(ref m);
                DeleteObject(rgn);
                rgn = CreateRectRgn(clientRect.Left, clientRect.Top,
                    clientRect.Right, clientRect.Bottom);
                SelectClipRgn(dc, rgn);

                using (var g = Graphics.FromHdc(dc))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var pen = new Pen(Enabled ? arrowColor : FluentTheme.DisabledText, 1.3f * DeviceDpi / 96f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        g.DrawLines(pen, arrow);
                    if (DropDownStyle == ComboBoxStyle.DropDown)
                        using (var b = new SolidBrush(innerBorderColor))
                            g.FillRectangle(b, innerInnerBorder);
                    Color stroke = active ? FluentTheme.Accent : outerBorderColor;
                    using var outline = new Pen(stroke);
                    DrawRoundedRectangle(g, outline, outerBorder, outerRadius, outerRadius);
                }
                if (shoulEndPaint)
                    EndPaint(Handle, ref ps);
                DeleteObject(rgn);
            }
            else
                base.WndProc(ref m);
        }

        private const int WM_PAINT = 0xF;
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int L, T, R, B;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public nint hdc;
            public bool fErase;
            public int rcPaint_left;
            public int rcPaint_top;
            public int rcPaint_right;
            public int rcPaint_bottom;
            public bool fRestore;
            public bool fIncUpdate;
            public int reserved1;
            public int reserved2;
            public int reserved3;
            public int reserved4;
            public int reserved5;
            public int reserved6;
            public int reserved7;
            public int reserved8;
        }
        [DllImport("user32.dll")]
        private static extern nint BeginPaint(nint hWnd,
            [In, Out] ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(nint hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("gdi32.dll")]
        public static extern int SelectClipRgn(nint hDC, nint hRgn);

        [DllImport("user32.dll")]
        public static extern int GetUpdateRgn(nint hwnd, nint hrgn, bool fErase);
        public enum RegionFlags
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3,
        }
        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(nint hObject);

        [DllImport("gdi32.dll")]
        private static extern nint CreateRectRgn(int x1, int y1, int x2, int y2);

        public static void DrawBindingItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ComboBox cmb) return;

            object obj = cmb.Items[e.Index];
            bool isSep = obj is BindingSeparator;

            Color back = isSep ? RForm.buttonSecond : RForm.buttonMain;
            if (!isSep && (e.State & DrawItemState.Selected) != 0)
                back = RForm.borderMain;

            using var backBrush = new SolidBrush(back);
            e.Graphics.FillRectangle(backBrush, e.Bounds);

            string text = obj.ToString() ?? string.Empty;
            Font font = isSep
                ? new Font(e.Font ?? SystemFonts.DefaultFont, FontStyle.Bold)
                : (e.Font ?? SystemFonts.DefaultFont);

            int indent = isSep ? 6 : 14;
            var textRect = new Rectangle(e.Bounds.X + indent, e.Bounds.Y,
                                         e.Bounds.Width - indent, e.Bounds.Height);

            using var foreBrush = new SolidBrush(RForm.foreMain);
            e.Graphics.DrawString(text, font, foreBrush, textRect,
                new StringFormat { LineAlignment = StringAlignment.Center });

            if (isSep) font.Dispose();
        }
    }

    public sealed class BindingItem
    {
        public ushort Code { get; }
        public string DisplayName { get; }
        public ushort[]? ComboKeys { get; }
        public BindingItem(ushort code, string name, ushort[]? comboKeys = null)
        { Code = code; DisplayName = name; ComboKeys = comboKeys; }
        public override string ToString() => DisplayName;
    }

    public sealed class BindingSeparator
    {
        public string Label { get; }
        public BindingSeparator(string label) { Label = label; }
        public override string ToString() => Label;
    }
}
