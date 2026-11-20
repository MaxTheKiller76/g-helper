using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public class RButton : Button
    {
        private Color borderColor = Color.Transparent;
        private bool activated;
        private readonly FluentTransition selection;
        private readonly FluentTransition hover;

        public int BorderRadius { get; set; } = 8;
        public bool Secondary { get; set; }
        public bool Borderless { get; set; }

        public Color BorderColor
        {
            get => borderColor;
            set
            {
                if (borderColor == value) return;
                borderColor = value;
                UpdateInteractionColors();
                Invalidate();
            }
        }

        public bool Activated
        {
            get => activated;
            set
            {
                if (activated == value) return;
                activated = value;
                AccessibleDescription = value ? "Active" : null;
                if (Borderless) selection.SnapTo(value ? 1f : 0f);
                else selection.AnimateTo(value ? 1f : 0f);
                Invalidate();
            }
        }

        private Color SelectionColor => FluentTheme.HighContrast || borderColor.A == 0 ? FluentTheme.Accent : borderColor;
        private float SelectionProgress => Enabled && !FluentTheme.HighContrast && selection != null
            ? selection.Value : activated ? 1f : 0f;

        private Color SelectionFill
        {
            get
            {
                Color resting = base.BackColor;
                float progress = SelectionProgress;
                return progress > 0f && Enabled && !Borderless && !RForm.flatTheme && !FluentTheme.HighContrast
                    ? FluentTheme.Blend(resting, SelectionColor, 0.17f * progress)
                    : resting;
            }
        }

        public override Color BackColor
        {
            get
            {
                Color background = SelectionFill;
                float amount = hover?.Value ?? 0f;
                return amount > 0f && UsesHoverFill(background)
                    ? FluentTheme.Blend(background, InteractionTint(background), 0.06f * amount)
                    : background;
            }
            set => base.BackColor = value;
        }

        protected override bool ShowFocusCues => false;

        public RButton()
        {
            selection = new(this, onFrame: UpdateInteractionColors);
            hover = new(this, onFrame: UpdateInteractionColors);
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColorChanged += (_, _) => UpdateInteractionColors();
            UpdateInteractionColors();
        }

        private void UpdateInteractionColors()
        {
            Color background = SelectionFill;
            Color tint = InteractionTint(background);
            FlatAppearance.MouseOverBackColor = UsesHoverFill(background) ? BackColor : FluentTheme.Blend(background, tint, 0.06f);
            FlatAppearance.MouseDownBackColor = FluentTheme.Blend(background, tint, 0.13f);
        }

        private bool UsesHoverFill(Color color) => Enabled && !FluentTheme.HighContrast && color.A == 255;

        private static Color InteractionTint(Color color) => color.GetBrightness() > 0.5f ? Color.Black : Color.White;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (UsesHoverFill(SelectionFill)) hover.AnimateTo(1f, FluentTransition.Quick);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover.AnimateTo(0f, FluentTransition.Quick);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            SettleVisuals();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled) SettleVisuals();
            UpdateInteractionColors();
        }

        private void SettleVisuals()
        {
            selection?.SnapTo(activated ? 1f : 0f);
            hover?.SnapTo(0f);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled || FluentTheme.HighContrast)
                base.OnPaint(e);
            else
                PaintDisabledContent(e);
            if (Parent is null || ClientSize.Width < 3 || ClientSize.Height < 3) return;

            var bounds = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            int radius = RForm.flatTheme || BorderRadius == 0 ? 0 : FluentTheme.Scale(this, 4);
            var graphics = e.Graphics;
            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var outline = RComboBox.RoundedRect(bounds, radius, radius);

            if (radius > 0)
            {
                // Leave the native button rectangular for hit testing.
                using var corners = new GraphicsPath(FillMode.Alternate);
                corners.AddRectangle(ClientRectangle);
                corners.AddPath(outline, false);
                using var parentBrush = new SolidBrush(Parent.BackColor);
                graphics.FillPath(parentBrush, corners);
            }

            float selectionProgress = SelectionProgress;
            if (!Borderless && (!RForm.flatTheme || selectionProgress > 0f || FluentTheme.HighContrast))
            {
                Color stroke = Enabled ? FlatAppearance.BorderColor : FluentTheme.CardStroke;
                if (stroke.A == 0) stroke = FluentTheme.Stroke;
                if (selectionProgress > 0f && Enabled)
                    stroke = FluentTheme.Blend(stroke, SelectionColor, 0.55f * selectionProgress);
                using var pen = new Pen(stroke);
                graphics.DrawPath(pen, outline);
            }

            if (selectionProgress > 0f && !Borderless && bounds.Height >= FluentTheme.Scale(this, 12))
            {
                float length = Math.Min(FluentTheme.Scale(this, 14), bounds.Height / 2) * (0.45f + 0.55f * selectionProgress);
                float y = (bounds.Height - length) / 2f;
                int x = RightToLeft == RightToLeft.Yes ? bounds.Right - FluentTheme.Scale(this, 3) : FluentTheme.Scale(this, 3);
                Color color = Enabled ? SelectionColor : FluentTheme.DisabledText;
                using var marker = new Pen(Color.FromArgb((int)(255 * selectionProgress), color), FluentTheme.Scale(this, 2))
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                graphics.DrawLine(marker, x, y, x, y + length);
            }

            if (Focused && base.ShowFocusCues)
            {
                bounds.Inflate(-2, -2);
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    using var focus = new Pen(FluentTheme.Accent, FluentTheme.Scale(this, 2));
                    RComboBox.DrawRoundedRectangle(graphics, focus, bounds, Math.Max(0, radius - 1), Math.Max(0, radius - 1));
                }
            }

            graphics.Restore(state);
        }

        private void PaintDisabledContent(PaintEventArgs e)
        {
            using var background = new SolidBrush(BackColor);
            e.Graphics.FillRectangle(background, e.ClipRectangle);

            int gap = FluentTheme.Scale(this, 4);
            var content = Rectangle.FromLTRB(Padding.Left + gap, Padding.Top + 2,
                ClientSize.Width - Padding.Right - gap, ClientSize.Height - Padding.Bottom - 2);
            if (content.Width <= 0 || content.Height <= 0) return;

            var label = content;
            if (Image is Image image)
            {
                var icon = content;
                if (!string.IsNullOrEmpty(Text))
                {
                    switch (TextImageRelation)
                    {
                        case TextImageRelation.ImageAboveText:
                        case TextImageRelation.TextAboveImage:
                            int height = Math.Min(image.Height, content.Height / 2);
                            icon.Height = height;
                            label.Height = Math.Max(0, content.Height - height - gap);
                            if (TextImageRelation == TextImageRelation.ImageAboveText) label.Y = icon.Bottom + gap;
                            else icon.Y = label.Bottom + gap;
                            break;
                        case TextImageRelation.ImageBeforeText:
                        case TextImageRelation.TextBeforeImage:
                            int width = Math.Min(image.Width, content.Width / 2);
                            icon.Width = width;
                            label.Width = Math.Max(0, content.Width - width - gap);
                            bool imageFirst = (TextImageRelation == TextImageRelation.ImageBeforeText) != (RightToLeft == RightToLeft.Yes);
                            if (imageFirst) label.X = icon.Right + gap;
                            else icon.X = label.Right + gap;
                            break;
                    }
                }

                var state = e.Graphics.Save();
                e.Graphics.SetClip(icon, CombineMode.Intersect);
                ControlPaint.DrawImageDisabled(e.Graphics, image, icon.X + (icon.Width - image.Width) / 2,
                    icon.Y + (icon.Height - image.Height) / 2, BackColor);
                e.Graphics.Restore(state);
            }

            if (label.Width <= 0 || label.Height <= 0) return;
            TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
            flags |= TextAlign switch
            {
                ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
                ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
                _ => TextFormatFlags.HorizontalCenter
            };
            flags |= TextAlign switch
            {
                ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
                ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => TextFormatFlags.Bottom,
                _ => TextFormatFlags.VerticalCenter
            };
            if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
            else if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            if (RightToLeft == RightToLeft.Yes) flags |= TextFormatFlags.RightToLeft;
            TextRenderer.DrawText(e.Graphics, Text, Font, label, FluentTheme.DisabledText, flags);
        }
    }
}
