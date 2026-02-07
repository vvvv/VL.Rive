using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;
using SpanExtensions = VL.Rive.Interop.SpanExtensions;

namespace VL.Rive.Internal;

internal sealed class RiveStringValue : RiveValue<string?>
{
    public RiveStringValue(nint handle) : base(handle, RiveDataType.String) { }

    public override unsafe string? TypedValue
    {
        get => SpanExtensions.AsString(rive_ViewModelInstanceStringRuntime_Value(handle));
        set
        {
            if (value is null)
                value = string.Empty;
            using var ms = new MarshaledString(value);
            rive_ViewModelInstanceStringRuntime_SetValue(handle, ms);
        }
    }
}
