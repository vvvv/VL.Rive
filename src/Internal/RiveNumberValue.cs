using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal sealed class RiveNumberValue : RiveValue<float>
{
    public RiveNumberValue(nint handle) : base(handle, RiveDataType.Number) { }

    public override float TypedValue
    {
        get => rive_ViewModelInstanceNumberRuntime_Value(handle);
        set => rive_ViewModelInstanceNumberRuntime_SetValue(handle, value);
    }
}
