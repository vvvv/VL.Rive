using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;
using SpanExtensions = VL.Rive.Interop.SpanExtensions;

namespace VL.Rive.Internal;

internal sealed class RiveEnumValue : RiveValue<string?>
{
    public RiveEnumValue(nint handle) : base(handle, RiveDataType.EnumType) { }

    public override unsafe string? TypedValue
    {
        get => SpanExtensions.AsString(rive_ViewModelInstanceEnumRuntime_Value(handle));
        set
        {
            if (value is null)
                value = string.Empty;
            using var ms = new MarshaledString(value);
            rive_ViewModelInstanceEnumRuntime_SetValue(handle, ms);
        }
    }
}
