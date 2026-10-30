using System.Runtime.InteropServices;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;
using SpanExtensions = VL.Rive.Interop.SpanExtensions;

namespace VL.Rive.Internal;

internal sealed class RiveEnumValue : RiveValue<string?>
{
    public RiveEnumValue(nint handle) : base(handle, RiveDataType.EnumType) { }

    public override unsafe string? TypedValue
    {
        get
        {
            // Native side returns a copy that we own
            var ptr = rive_ViewModelInstanceEnumRuntime_Value(handle);
            try
            {
                return SpanExtensions.AsString(ptr);
            }
            finally
            {
                NativeMemory.Free(ptr);
            }
        }
        set
        {
            if (value is null)
                value = string.Empty;
            using var ms = new MarshaledString(value);
            rive_ViewModelInstanceEnumRuntime_SetValue(handle, ms);
        }
    }
}
