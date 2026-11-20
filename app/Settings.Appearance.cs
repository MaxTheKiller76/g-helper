using GHelper.UI;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace GHelper
{
    public partial class SettingsForm
    {
        private DashboardHeading? dashboardHeading;

        protected override Padding AcrylicInsets => new(Padding.Left,
            Padding.Top + (dashboardHeading?.Height ?? 0), Padding.Right, Padding.Bottom);

        private void InitializeDashboardHeading()
        {
            int iconSize = FluentTheme.Scale(this, 32);
            using Icon? applicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            using var icon = new Icon(applicationIcon ?? Properties.Resources.standard, iconSize, iconSize);
            dashboardHeading = new DashboardHeading(AppConfig.GetModelShort(), icon.ToBitmap())
            {
                Dock = DockStyle.Top,
                Height = FluentTheme.Scale(this, 68)
            };
            Controls.Add(dashboardHeading);
        }

        internal void FitDashboardTo(Rectangle workArea)
        {
            int margin = FluentTheme.Scale(this, 20);
            int width = Math.Max(1, workArea.Width - margin);
            int height = Math.Max(MinimumSize.Height, workArea.Height - margin);
            MaximumSize = new Size(width, height);
            MinimumSize = new Size(Math.Min(width, FluentTheme.Scale(this, 452)), MinimumSize.Height);
        }

        private sealed class DashboardHeading : Control
        {
            private const string Title = "G-Helper";
            private readonly string device;
            private readonly Bitmap emblem;
            private readonly Font titleFont = new("Segoe UI Semibold", 18F);
            private readonly FluentTransition reveal;

            public DashboardHeading(string device, Bitmap emblem)
            {
                this.device = device;
                this.emblem = emblem;
                reveal = new(this, 1f);
                DoubleBuffered = true;
                AccessibleRole = AccessibleRole.StaticText;
                AccessibleName = Title + ", " + device;
                TabStop = false;
            }

            protected override void OnVisibleChanged(EventArgs e)
            {
                base.OnVisibleChanged(e);
                if (Visible) reveal?.PlayFrom(0f, 1f, FluentTransition.Entrance);
                else reveal?.SnapTo(1f);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                if (FindForm() is RForm { HasAcrylicFrame: true })
                    AcrylicBackdrop.Clear(e.Graphics, e.ClipRectangle);
                else
                    base.OnPaintBackground(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                int Scale(int dips) => FluentTheme.Scale(this, dips);

                var graphics = e.Graphics;
                float progress = reveal.Value;
                float opacity = 0.70f + 0.30f * progress;
                int alpha = (int)Math.Round(255 * opacity);
                var state = graphics.Save();
                try
                {
                    graphics.TranslateTransform(0, Scale(8) * (1f - progress));
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    int iconSize = Scale(32);
                    var iconBounds = new Rectangle(Scale(5), Scale(14), iconSize, iconSize);
                    if (alpha == 255)
                    {
                        graphics.DrawImage(emblem, iconBounds);
                    }
                    else
                    {
                        using var attributes = new ImageAttributes();
                        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = opacity });
                        graphics.DrawImage(emblem, iconBounds, 0, 0, emblem.Width, emblem.Height,
                            GraphicsUnit.Pixel, attributes);
                    }

                    int left = Scale(51);
                    int width = ClientSize.Width - left - Scale(4);
                    if (width <= 0) return;

                    using var format = new StringFormat { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                    using var foreground = new SolidBrush(Color.FromArgb(alpha, FluentTheme.Text));
                    using var caption = new SolidBrush(Color.FromArgb(alpha, FluentTheme.SecondaryText));
                    graphics.DrawString(Title, titleFont, foreground, new Rectangle(left, Scale(5), width, Scale(32)), format);
                    graphics.DrawString(device, Font, caption, new Rectangle(left, Scale(37), width, Scale(21)), format);
                }
                finally { graphics.Restore(state); }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    emblem.Dispose();
                    titleFont.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
