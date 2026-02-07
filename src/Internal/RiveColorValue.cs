using Stride.Core.Mathematics;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal sealed class RiveColorValue : RiveValue<Color4>
{
    public RiveColorValue(nint handle) : base(handle, RiveDataType.Color) { }

    public override Color4 TypedValue
    {
        get
        {
            var color = rive_ViewModelInstanceColorRuntime_Value(handle);
            var bgra = new Color4(color);
            return new Color4(bgra.ToRgba());
        }
        set
        {
            rive_ViewModelInstanceColorRuntime_SetValue(handle, (uint)value.ToBgra());
        }
    }
}
