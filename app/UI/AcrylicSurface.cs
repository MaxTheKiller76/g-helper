using System.Runtime.InteropServices;

namespace GHelper.UI
{
    internal sealed class AcrylicSurface : IDisposable
    {
        private readonly Dictionary<Control, PaintWindow> windows = new();
        public bool HasLiveBackdrop { get; set; }

        public AcrylicSurface(Control root) => Attach(root);

        private void Attach(Control control)
        {
            windows.Add(control, new PaintWindow(control, this));
            control.ControlAdded += ChildAdded;
            control.ControlRemoved += ChildRemoved;
            foreach (Control child in control.Controls) Attach(child);
        }

        private void Detach(Control control)
        {
            if (!windows.Remove(control, out var window)) return;
            control.ControlAdded -= ChildAdded;
            control.ControlRemoved -= ChildRemoved;
            window.Dispose();
            foreach (Control child in control.Controls) Detach(child);
        }

        private void ChildAdded(object? sender, ControlEventArgs e)
        {
            if (e.Control is { } child && !windows.ContainsKey(child)) Attach(child);
        }

        private void ChildRemoved(object? sender, ControlEventArgs e)
        {
            if (e.Control is { } child) Detach(child);
        }

        public void Dispose()
        {
            foreach (var (control, window) in windows)
            {
                control.ControlAdded -= ChildAdded;
                control.ControlRemoved -= ChildRemoved;
                window.Dispose();
            }
            windows.Clear();
        }

        private sealed class PaintWindow : NativeWindow, IDisposable
        {
            private const int Paint = 0x000f;
            private const int EraseBackground = 0x0014;
            private const int PrintClient = 0x0318;
            private const int PrintClientArea = 0x0004;
            private const int PrintBackground = 0x0008;
            private const uint CopyPixels = 0x00cc0020;

            private readonly Control control;
            private readonly AcrylicSurface acrylicSurface;
            private Size bufferSize;
            private nint buffer, bitmap, pixels, originalBitmap;

            public PaintWindow(Control control, AcrylicSurface acrylicSurface)
            {
                this.control = control;
                this.acrylicSurface = acrylicSurface;
                control.HandleCreated += HandleCreated;
                control.HandleDestroyed += HandleDestroyed;
                if (control.IsHandleCreated) AssignHandle(control.Handle);
            }

            private void HandleCreated(object? sender, EventArgs e) => AssignHandle(control.Handle);

            private void HandleDestroyed(object? sender, EventArgs e)
            {
                if (Handle != nint.Zero) ReleaseHandle();
                ReleaseBuffer();
            }

            protected override unsafe void WndProc(ref Message message)
            {
                if (message.Msg == EraseBackground)
                {
                    message.Result = 1;
                    return;
                }

                Size size = control.ClientSize;
                if (message.Msg != Paint || message.WParam != nint.Zero || control.IsDisposed ||
                    size.Width <= 0 || size.Height <= 0 || !PrepareBuffer(size))
                {
                    base.WndProc(ref message);
                    return;
                }

                nint hwnd = message.HWnd;
                nint target = BeginPaint(hwnd, out var paint);
                if (target == nint.Zero) return;
                try
                {
                    if (paint.Right <= paint.Left || paint.Bottom <= paint.Top) return;

                    Color background = control.BackColor.A == 255 ? control.BackColor : FluentTheme.Canvas;
                    var surface = new Span<uint>((void*)pixels, size.Width * size.Height);
                    GdiFlush();
                    surface.Fill(unchecked((uint)background.ToArgb()));
                    // The combo already supports painting to an explicit DC, including its Fluent frame.
                    var render = Message.Create(hwnd, control is RComboBox ? Paint : PrintClient,
                        buffer, PrintClientArea | PrintBackground);
                    int dcState = SaveDC(buffer);
                    try { base.WndProc(ref render); }
                    finally { if (dcState != 0) RestoreDC(buffer, dcState); }
                    GdiFlush();
                    ApplyOpacity(surface, background);

                    foreach (Control child in control.Controls)
                        if (child.Visible && child.IsHandleCreated)
                            ExcludeClipRect(target, child.Left, child.Top, child.Right, child.Bottom);

                    BitBlt(target, 0, 0, size.Width, size.Height, buffer, 0, 0, CopyPixels);
                    message.Result = nint.Zero;
                }
                finally { EndPaint(hwnd, ref paint); }
            }

            private void ApplyOpacity(Span<uint> surface, Color background)
            {
                // The live blur has no built-in tint. Add more of the theme color to keep text readable.
                uint canvasOpacity = acrylicSurface.HasLiveBackdrop ? 169u : 130u;
                uint cardOpacity = acrylicSurface.HasLiveBackdrop ? 186u : 161u;
                uint inputOpacity = acrylicSurface.HasLiveBackdrop ? 209u : 198u;
                uint strokeOpacity = acrylicSurface.HasLiveBackdrop ? 207u : 192u;
                const uint opaque = 255;

                uint canvas = Rgb(FluentTheme.Canvas);
                uint card = Rgb(FluentTheme.Card);
                uint fill = Rgb(FluentTheme.ControlFill);
                uint cardStroke = Rgb(FluentTheme.CardStroke);
                uint stroke = Rgb(FluentTheme.Stroke);
                uint local = Rgb(background);
                uint parent = Rgb(control.Parent?.BackColor ?? FluentTheme.Canvas);
                bool input = control is ButtonBase or ComboBox or TextBoxBase or UpDownBase;
                uint localOpacity = input ? inputOpacity : cardOpacity;
                bool dark = FluentTheme.Canvas.GetBrightness() < 0.5f;
                uint hover = uint.MaxValue;
                uint pressed = uint.MaxValue;
                if (control is ButtonBase button)
                {
                    if (!button.FlatAppearance.MouseOverBackColor.IsEmpty) hover = Rgb(button.FlatAppearance.MouseOverBackColor);
                    if (!button.FlatAppearance.MouseDownBackColor.IsEmpty) pressed = Rgb(button.FlatAppearance.MouseDownBackColor);
                }

                // GDI draws straight RGB. The compositor expects premultiplied BGRA.
                // Give surface fills distinct opacity levels and retain full coverage for content.
                for (int i = 0; i < surface.Length; i++)
                {
                    uint source = surface[i];
                    uint color = source & 0xffffff;
                    if (source == 0 && dark) color = parent;

                    uint alpha = input && (color == local || color == hover || color == pressed) ? inputOpacity
                        : color == canvas ? canvasOpacity
                        : color == card ? cardOpacity
                        : color == fill ? inputOpacity
                        : color == cardStroke || color == stroke ? strokeOpacity
                        : color == local ? localOpacity
                        : Math.Min(EdgeOpacity(color, local, localOpacity),
                            Math.Min(EdgeOpacity(color, canvas, canvasOpacity), EdgeOpacity(color, card, cardOpacity)));

                    surface[i] = alpha == opaque ? color | 0xff000000 :
                        (alpha << 24) | (((color >> 16) * alpha / opaque) << 16) |
                        ((((color >> 8) & 255) * alpha / opaque) << 8) | ((color & 255) * alpha / opaque);
                }
            }

            private static uint EdgeOpacity(uint color, uint surface, uint opacity)
            {
                const int edgeRange = 24;
                int red = Math.Abs((int)(color >> 16) - (int)(surface >> 16));
                int green = Math.Abs((int)((color >> 8) & 255) - (int)((surface >> 8) & 255));
                int blue = Math.Abs((int)(color & 255) - (int)(surface & 255));
                int distance = Math.Min(edgeRange, Math.Max(red, Math.Max(green, blue)));
                return opacity + (255 - opacity) * (uint)distance / edgeRange;
            }

            private static uint Rgb(Color color) => unchecked((uint)color.ToArgb()) & 0xffffff;

            private bool PrepareBuffer(Size size)
            {
                if (bufferSize == size) return true;
                ReleaseBuffer();

                buffer = CreateCompatibleDC(nint.Zero);
                if (buffer == nint.Zero) return false;

                var info = new BitmapInfo { Size = 40, Width = size.Width, Height = -size.Height, Planes = 1, BitCount = 32 };
                bitmap = CreateDIBSection(buffer, ref info, 0, out pixels, nint.Zero, 0);
                if (bitmap != nint.Zero && pixels != nint.Zero)
                    originalBitmap = SelectObject(buffer, bitmap);

                if (originalBitmap == nint.Zero || originalBitmap == -1)
                {
                    ReleaseBuffer();
                    return false;
                }

                bufferSize = size;
                return true;
            }

            private void ReleaseBuffer()
            {
                if (originalBitmap != nint.Zero && originalBitmap != -1) SelectObject(buffer, originalBitmap);
                if (bitmap != nint.Zero) DeleteObject(bitmap);
                if (buffer != nint.Zero) DeleteDC(buffer);
                buffer = bitmap = pixels = originalBitmap = nint.Zero;
                bufferSize = Size.Empty;
            }

            public void Dispose()
            {
                control.HandleCreated -= HandleCreated;
                control.HandleDestroyed -= HandleDestroyed;
                if (Handle != nint.Zero) ReleaseHandle();
                ReleaseBuffer();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct PaintStruct
        {
            public nint Hdc;
            public int Erase, Left, Top, Right, Bottom, Restore, IncUpdate;
            public fixed byte Reserved[32];
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            public uint Size;
            public int Width, Height;
            public ushort Planes, BitCount;
            public uint Compression, ImageSize;
            public int XResolution, YResolution;
            public uint ColorsUsed, ColorsImportant;
        }

        [DllImport("user32.dll")] private static extern nint BeginPaint(nint window, out PaintStruct paint);
        [DllImport("user32.dll")] private static extern bool EndPaint(nint window, ref PaintStruct paint);
        [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
        [DllImport("gdi32.dll")] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);
        [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint item);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint item);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
        [DllImport("gdi32.dll")] private static extern int SaveDC(nint dc);
        [DllImport("gdi32.dll")] private static extern bool RestoreDC(nint dc, int state);
        [DllImport("gdi32.dll")] private static extern int ExcludeClipRect(nint dc, int left, int top, int right, int bottom);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(nint dest, int x, int y, int width, int height, nint source, int srcX, int srcY, uint operation);
        [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    }
}
