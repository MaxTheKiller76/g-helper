using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    internal sealed class FluentToggle : CheckBox
    {
        private const int TrackWidth = 32;
        private const int TrackHeight = 16;
        private const int LabelGap = 8;
        private readonly FluentTransition slide;

        public FluentToggle()
        {
            slide = new(this);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            slide?.AnimateTo(Checked ? 1f : 0f);
            base.OnCheckedChanged(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) slide?.SnapTo(Checked ? 1f : 0f);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled) slide?.SnapTo(Checked ? 1f : 0f);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine);
            return new Size(text.Width + FluentTheme.Scale(this, TrackWidth + LabelGap) + Padding.Horizontal,
                Math.Max(text.Height, FluentTheme.Scale(this, TrackHeight + 4)) + Padding.Vertical);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int width = FluentTheme.Scale(this, TrackWidth);
            int height = FluentTheme.Scale(this, TrackHeight);
            int gap = FluentTheme.Scale(this, LabelGap);
            bool rtl = RightToLeft == RightToLeft.Yes;
            var track = new Rectangle(rtl ? ClientSize.Width - width - Padding.Right : Padding.Left,
                (ClientSize.Height - height) / 2, width - 1, height - 1);
            Color text = Enabled ? ForeColor : FluentTheme.DisabledText;
            bool highContrast = FluentTheme.HighContrast;
            float position = highContrast ? (Checked ? 1f : 0f) : slide.Value;
            float checkedFill = Enabled ? position : 0f;
            Color stroke = checkedFill > 0f ? FluentTheme.Blend(text, FluentTheme.Accent, checkedFill) : text;
            Color fill = checkedFill > 0f ? FluentTheme.Blend(BackColor, FluentTheme.Accent, checkedFill) : BackColor;
            if (Enabled && !highContrast && ClientRectangle.Contains(PointToClient(Cursor.Position)))
                fill = FluentTheme.Blend(fill, text, MouseButtons == MouseButtons.Left ? 0.16f : 0.08f);
            Color thumb = checkedFill > 0f
                ? highContrast ? SystemColors.HighlightText : fill.GetBrightness() > 0.56f ? Color.Black : Color.White
                : text;

            var graphics = e.Graphics;
            using var background = new SolidBrush(BackColor);
            graphics.FillRectangle(background, e.ClipRectangle);
            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var shell = RComboBox.RoundedRect(track, height / 2, height / 2);
            using var trackBrush = new SolidBrush(fill);
            using var trackOutline = new Pen(stroke);
            using var thumbBrush = new SolidBrush(thumb);
            graphics.FillPath(trackBrush, shell);
            graphics.DrawPath(trackOutline, shell);
            int diameter = FluentTheme.Scale(this, 10);
            int inset = FluentTheme.Scale(this, 3);
            float travel = Math.Max(0, track.Width - 2 * inset - diameter);
            float thumbX = track.Left + inset + travel * (rtl ? 1f - position : position);
            graphics.FillEllipse(thumbBrush, thumbX, track.Top + (height - diameter) / 2, diameter, diameter);
            graphics.Restore(state);

            int textX = rtl ? Padding.Left : track.Right + gap;
            int textRight = rtl ? track.Left - gap : ClientSize.Width - Padding.Right;
            if (textRight > textX)
            {
                var labelBounds = new Rectangle(textX, 0, textRight - textX, ClientSize.Height);
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
                if (rtl) flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;
                if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
                TextRenderer.DrawText(graphics, Text, Font, labelBounds, text, flags);
            }

            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(graphics, ClientRectangle, text, BackColor);
        }
    }
}
