using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using static GHelper.UI.CompositionInterop;

namespace GHelper.UI
{
    // Windows.Graphics.Effects interfaces used by ICompositor.CreateEffectFactory.
    [GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
    [Guid("af86e2e0-b12d-4c6a-9c5a-d7aa65101e90")]
    internal partial interface IWinRtInspectable
    {
        [PreserveSig] int GetIids(out uint count, out nint values);
        [PreserveSig] int GetRuntimeClassName(out nint value);
        [PreserveSig] int GetTrustLevel(out int value);
    }

    [GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
    [Guid("cb51c0ce-8fe6-4636-b202-861faa07d8f3")]
    internal partial interface IGraphicsEffect : IWinRtInspectable
    {
        [PreserveSig] int GetName(out nint name);
        [PreserveSig] int PutName(nint name);
    }

    [GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
    [Guid("2d8f9ddc-4339-4eb9-9216-f9deb75658a2")]
    internal partial interface IGraphicsEffectSource : IWinRtInspectable { }

    [GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
    [Guid("2fc57384-a068-44d7-a331-30982fcf7177")]
    internal partial interface IGraphicsEffectD2D1Interop
    {
        [PreserveSig] int GetEffectId(out Guid id);
        [PreserveSig] int GetNamedPropertyMapping(nint name, out uint index, out int mapping);
        [PreserveSig] int GetPropertyCount(out uint count);
        [PreserveSig] int GetProperty(uint index, out nint value);
        [PreserveSig] int GetSource(uint index, out nint value);
        [PreserveSig] int GetSourceCount(out uint count);
    }

    [GeneratedComClass]
    internal sealed unsafe partial class GaussianBackdropEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
    {
        private const int InvalidArgument = unchecked((int)0x80070057);
        private const int NotImplemented = unchecked((int)0x80004001);
        private const int OutOfMemory = unchecked((int)0x8007000e);
        private const int CreateUInt32 = FirstInspectableMethod + 5;
        private const int CreateSingle = FirstInspectableMethod + 8;
        private const float BlurRadius = 26f;
        private const uint BalancedOptimization = 1;
        private const uint HardBorder = 1;
        private static readonly Guid BlurId = new("1feb6d69-2fe6-4ac9-8c58-1d7f93e7a6a5");
        private static readonly Guid PropertyValueStaticsId = new("629bdbc8-d932-4ff4-96b9-8d96c5c1e858");
        private static readonly Guid GraphicsEffectId = typeof(IGraphicsEffect).GUID;
        internal static readonly Guid SourceId = typeof(IGraphicsEffectSource).GUID;

        // Keep the agile WinRT objects alive for as long as the compositor holds the effect description.
        private readonly ComReference source;
        private readonly ComReference radius;
        private readonly ComReference optimization;
        private readonly ComReference border;

        public GaussianBackdropEffect(ComReference effectSource)
        {
            source = effectSource.Query(SourceId);
            using var properties = GetFactory("Windows.Foundation.PropertyValue", PropertyValueStaticsId);
            nint boxedRadius;
            Check(((delegate* unmanaged[Stdcall]<nint, float, nint*, int>)Method(properties.Pointer, CreateSingle))(
                properties.Pointer, BlurRadius, &boxedRadius));
            radius = new(boxedRadius);
            optimization = BoxUInt32(properties, BalancedOptimization);
            border = BoxUInt32(properties, HardBorder);
        }

        internal ComReference GetInterface() => new((nint)ComInterfaceMarshaller<IGraphicsEffect>.ConvertToUnmanaged(this));

        private static ComReference BoxUInt32(ComReference factory, uint value)
        {
            nint result;
            Check(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(factory.Pointer, CreateUInt32))(
                factory.Pointer, value, &result));
            return new(result);
        }

        public int GetIids(out uint count, out nint values)
        {
            count = 0;
            values = 0;
            try { values = Marshal.AllocCoTaskMem(2 * sizeof(Guid)); }
            catch (OutOfMemoryException) { return OutOfMemory; }
            ((Guid*)values)[0] = GraphicsEffectId;
            ((Guid*)values)[1] = SourceId;
            count = 2;
            return 0;
        }

        public int GetRuntimeClassName(out nint value) { value = 0; return 0; }
        public int GetTrustLevel(out int value) { value = 0; return 0; }
        public int GetName(out nint name) { name = 0; return 0; }
        public int PutName(nint name) => NotImplemented;
        public int GetEffectId(out Guid id) { id = BlurId; return 0; }
        public int GetNamedPropertyMapping(nint name, out uint index, out int mapping)
        {
            index = 0;
            mapping = 0;
            return InvalidArgument;
        }
        public int GetPropertyCount(out uint count) { count = 3; return 0; }
        public int GetSourceCount(out uint count) { count = 1; return 0; }

        public int GetProperty(uint index, out nint value) => CopyReference(index switch
        {
            0 => radius,
            1 => optimization,
            2 => border,
            _ => null
        }, out value);

        public int GetSource(uint index, out nint value) => CopyReference(index == 0 ? source : null, out value);

        private static int CopyReference(ComReference? reference, out nint value)
        {
            value = reference?.Pointer ?? 0;
            if (value == 0) return InvalidArgument;
            Marshal.AddRef(value);
            return 0;
        }
    }
}
