using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace GHelper.UI
{
    // These entry points use the Windows SDK WinRT ABI. The app needs neither WinUI nor Win2D at runtime.
    internal static unsafe class CompositionInterop
    {
        internal const int FirstInspectableMethod = 6;

        internal static nint Method(nint instance, int slot) => (*(nint**)instance)[slot];

        internal static ComReference GetObject(ComReference instance, int slot)
        {
            nint result;
            Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(instance.Pointer, slot))(instance.Pointer, &result));
            return new(result);
        }

        internal static ComReference GetObject(ComReference instance, int slot, nint argument)
        {
            nint result;
            Check(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)Method(instance.Pointer, slot))(instance.Pointer, argument, &result));
            return new(result);
        }

        internal static void SetObject(ComReference instance, int slot, nint value) =>
            Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)Method(instance.Pointer, slot))(instance.Pointer, value));

        internal static ComReference Activate(string className)
        {
            using var name = new WinRtString(className);
            Check(RoActivateInstance(name.Pointer, out nint result));
            return new(result);
        }

        internal static ComReference GetFactory(string className, Guid iid)
        {
            using var name = new WinRtString(className);
            Check(RoGetActivationFactory(name.Pointer, in iid, out nint result));
            return new(result);
        }

        internal static void Check(int hresult, [CallerMemberName] string? operation = null)
        {
            if (hresult < 0) throw new COMException($"Windows composition: {operation} ({hresult:X8})", hresult);
        }

        internal sealed class ComReference : SafeHandle
        {
            public ComReference(nint pointer) : base(nint.Zero, ownsHandle: true) => SetHandle(pointer);

            public nint Pointer => handle;
            public override bool IsInvalid => handle == 0;

            public ComReference Query(Guid iid)
            {
                Check(Marshal.QueryInterface(handle, in iid, out nint result));
                return new(result);
            }

            protected override bool ReleaseHandle()
            {
                Marshal.Release(handle);
                return true;
            }
        }

        internal readonly ref struct WinRtString
        {
            public nint Pointer { get; }

            public WinRtString(string text)
            {
                Check(WindowsCreateString(text, text.Length, out nint value));
                Pointer = value;
            }

            public void Dispose() => WindowsDeleteString(Pointer);
        }

        [DllImport("combase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int WindowsCreateString(string source, int length, out nint value);

        [DllImport("combase.dll")]
        private static extern int WindowsDeleteString(nint value);

        [DllImport("combase.dll")]
        private static extern int RoActivateInstance(nint className, out nint value);

        [DllImport("combase.dll")]
        private static extern int RoGetActivationFactory(nint className, in Guid iid, out nint factory);
    }
}
