using System.Runtime.InteropServices;

namespace GHelper.UI
{
    public class CustomMenuRenderer : ToolStripProfessionalRenderer
    {
        public CustomMenuRenderer() : base(new ProfessionalColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
            Color bg = e.Item.Owner?.BackColor ?? SystemColors.Menu;

            using (var background = new SolidBrush(bg))
                e.Graphics.FillRectangle(background, bounds);

            if (!e.Item.Selected || !e.Item.Enabled) return;
            bounds.Inflate(-3, -1);
            Color selected = FluentTheme.HighContrast ? SystemColors.Highlight : FluentTheme.Blend(bg, FluentTheme.Text, 0.09f);
            using var highlight = new SolidBrush(selected);
            RComboBox.FillRoundedRectangle(e.Graphics, highlight, bounds, 4, 4);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Item is not ToolStripMenuItem { Checked: true }) return;

            Color color = e.Item.Selected && FluentTheme.HighContrast ? SystemColors.HighlightText : FluentTheme.Accent;

            var rect = new Rectangle(0, 0, e.ImageRectangle.Right + e.ImageRectangle.X, e.Item.Height);
            TextRenderer.DrawText(e.Graphics, "✓", e.Item.Font, rect, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
                e.TextColor = FluentTheme.DisabledText;
            else if (e.Item.Selected && FluentTheme.HighContrast)
                e.TextColor = SystemColors.HighlightText;
            else
                e.TextColor = e.Item.Owner?.ForeColor ?? SystemColors.MenuText;

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var pen = new Pen(FluentTheme.CardStroke);
            e.Graphics.DrawLine(pen, 6, y, e.Item.Width - 6, y);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var b = new SolidBrush(e.ToolStrip.BackColor);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // suppress the default white/light stripe drawn on the left margin
            using var b = new SolidBrush(e.ToolStrip.BackColor);
            e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }

    class CustomContextMenu : ContextMenuStrip
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(nint hwnd,
                                                            DWMWINDOWATTRIBUTE attribute,
                                                            ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                            uint cbAttribute);

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x00000008; return cp; }
        }

        public CustomContextMenu()
        {
            try
            {
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL;     //change as you want
                DwmSetWindowAttribute(Handle,
                                      DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                                      ref preference,
                                      sizeof(uint));
            }
            catch { }
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWA_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3,
        }
    }

}
