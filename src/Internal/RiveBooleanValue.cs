using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal sealed class RiveBooleanValue : RiveValue<bool>
{
    public RiveBooleanValue(nint handle) : base(handle, RiveDataType.Boolean) { }

    public override bool TypedValue
    {
        get => rive_ViewModelInstanceBooleanRuntime_Value(handle) != 0;
        set => rive_ViewModelInstanceBooleanRuntime_SetValue(handle, (byte)(value ? 1 : 0));
    }
}
