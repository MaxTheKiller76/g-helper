using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public class RCheckBox : CheckBox
    {
        private int borderRadius = 5;
        public int BorderRadius
        {
            get => borderRadius;
            set => borderRadius = value;
        }

        public RCheckBox()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (Parent is null || ClientSize.Width < 3 || ClientSize.Height < 3 || Appearance != Appearance.Button) return;

            int radius = RForm.flatTheme || borderRadius == 0 ? 0 : FluentTheme.Scale(this, 4);

            Rectangle borderRect = new Rectangle(0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
            Color sideColor = Checked && Enabled ? FluentTheme.Accent : FluentTheme.Stroke;

            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath roundedPath = RComboBox.RoundedRect(borderRect, radius, radius))
            using (GraphicsPath cutoutPath = new GraphicsPath())
            using (Brush parentBrush = new SolidBrush(Parent.BackColor))
            {
                cutoutPath.AddRectangle(ClientRectangle);
                cutoutPath.AddPath(roundedPath, false);
                cutoutPath.FillMode = FillMode.Alternate;
                pevent.Graphics.FillPath(parentBrush, cutoutPath);
            }

            using var outline = new Pen(sideColor);
            RComboBox.DrawRoundedRectangle(pevent.Graphics, outline, borderRect, radius, radius);
        }
    }
}
