using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    internal sealed class FluentCard : Panel
    {
        public int BottomGap { get; init; }

        public FluentCard()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var bounds = ClientRectangle;
            bounds.Width--;
            bounds.Height -= 1 + FluentTheme.Scale(this, BottomGap);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            using var parent = new SolidBrush(Parent?.BackColor ?? FluentTheme.Canvas);
            e.Graphics.FillRectangle(parent, e.ClipRectangle);
            var state = e.Graphics.Save();
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = RForm.flatTheme ? 0 : FluentTheme.Scale(this, 8);
            using var outline = RComboBox.RoundedRect(bounds, radius, radius);
            using var surface = new SolidBrush(BackColor);
            using var stroke = new Pen(FluentTheme.CardStroke);
            e.Graphics.FillPath(surface, outline);
            e.Graphics.DrawPath(stroke, outline);
            e.Graphics.Restore(state);
        }
    }
}
