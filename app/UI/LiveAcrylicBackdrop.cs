using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using static GHelper.UI.CompositionInterop;

namespace GHelper.UI
{
    internal sealed unsafe class LiveAcrylicBackdrop : IDisposable
    {
        private const int CreateDesktopWindowTarget = 3;
        private const int CreateEffectFactory = FirstInspectableMethod + 5;
        private const int CreateSpriteVisual = FirstInspectableMethod + 16;
        private const int CreateHostBackdropBrush = FirstInspectableMethod;
        private const int SetRoot = FirstInspectableMethod + 1;
        private const int SetBrush = FirstInspectableMethod + 1;
        private const int SetRelativeSizeAdjustment = FirstInspectableMethod + 5;
        private const int CreateEffectSourceParameter = FirstInspectableMethod;
        private const int CreateEffectBrush = FirstInspectableMethod;
        private const int SetSourceParameter = FirstInspectableMethod + 1;
        private static readonly Guid DesktopInteropId = new("29e691fa-4567-4dca-b319-d0f207eb6807");
        private static readonly Guid CompositorId = new("b403ca50-7f8c-4e83-985f-cc45060036d8");
        private static readonly Guid Compositor3Id = new("c9dd8ef0-6eb1-4e3c-a658-675d9c64d4ab");
        private static readonly Guid TargetId = new("a1bea8ba-d726-4663-8129-6b5e7927ffa6");
        private static readonly Guid BrushId = new("ab0d7608-30c0-40e9-b568-b60a6bd1fb46");
        private static readonly Guid VisualId = new("117e202d-a859-4c89-873b-c2aa566788e3");
        private static readonly Guid Visual2Id = new("3052b611-56c3-4c3e-8bf3-f6e1ad473f06");
        private static readonly Guid SourceParameterFactoryId = new("b3d9f276-aba3-4724-acf3-d0397464db1c");

        private readonly AcrylicBackgroundWindow backgroundWindow;
        private readonly ComReference target;

        private LiveAcrylicBackdrop(AcrylicBackgroundWindow backgroundWindow, ComReference target)
        {
            this.backgroundWindow = backgroundWindow;
            this.target = target;
        }

        public static LiveAcrylicBackdrop? TryCreate(nint foreground)
        {
            AcrylicBackgroundWindow? window = null;
            try
            {
                window = new AcrylicBackgroundWindow();
                if (window.EnableBackdrop())
                {
                    var backdrop = new LiveAcrylicBackdrop(window, Attach(window.Handle));
                    if (window.Follow(foreground)) return backdrop;
                    backdrop.Dispose();
                    return null;
                }
            }
            catch (Exception e) when (e is COMException or Win32Exception or DllNotFoundException or EntryPointNotFoundException)
            {
                Logger.WriteLine($"Live Acrylic backdrop: {e.Message}");
            }
            window?.Dispose();
            return null;
        }

        private static ComReference Attach(nint window)
        {
            var composition = WindowComposition.ForCurrentThread;
            using var desktop = composition.Compositor.Query(DesktopInteropId);
            nint desktopTarget;
            Check(((delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)Method(desktop.Pointer, CreateDesktopWindowTarget))(
                desktop.Pointer, window, 0, &desktopTarget));
            using var targetObject = new ComReference(desktopTarget);
            using var compositor = composition.Compositor.Query(CompositorId);
            using var root = GetObject(compositor, CreateSpriteVisual);
            using var rootVisual = root.Query(VisualId);
            using var relativeVisual = root.Query(Visual2Id);
            Check(((delegate* unmanaged[Stdcall]<nint, Vector2, int>)Method(relativeVisual.Pointer, SetRelativeSizeAdjustment))(
                relativeVisual.Pointer, Vector2.One));
            using var blur = CreateBlurBrush(composition.Compositor, compositor);
            SetObject(root, SetBrush, blur.Pointer);
            var target = targetObject.Query(TargetId);
            try { SetObject(target, SetRoot, rootVisual.Pointer); }
            catch { target.Dispose(); throw; }
            return target;
        }

        private static ComReference CreateBlurBrush(ComReference compositor, ComReference baseCompositor)
        {
            // DWM supplies the pixels behind the window. The compositor blurs them even when input focus moves away.
            using var hostCompositor = compositor.Query(Compositor3Id);
            using var hostBackdrop = GetObject(hostCompositor, CreateHostBackdropBrush);
            using var hostBrush = hostBackdrop.Query(BrushId);
            using var sourceName = new WinRtString("Backdrop");
            using var parameters = GetFactory("Windows.UI.Composition.CompositionEffectSourceParameter", SourceParameterFactoryId);
            using var parameter = GetObject(parameters, CreateEffectSourceParameter, sourceName.Pointer);
            using var effect = new GaussianBackdropEffect(parameter).GetInterface();
            using var factory = GetObject(baseCompositor, CreateEffectFactory, effect.Pointer);
            using var brush = GetObject(factory, CreateEffectBrush);
            Check(((delegate* unmanaged[Stdcall]<nint, nint, nint, int>)Method(brush.Pointer, SetSourceParameter))(
                brush.Pointer, sourceName.Pointer, hostBrush.Pointer));
            return brush.Query(BrushId);
        }

        public void UpdateWindow(nint foreground) => backgroundWindow.Follow(foreground);

        public void Dispose()
        {
            if (target.IsClosed) return;
            ((delegate* unmanaged[Stdcall]<nint, nint, int>)Method(target.Pointer, SetRoot))(target.Pointer, 0);
            target.Dispose();
            backgroundWindow.Dispose();
        }
    }
}
