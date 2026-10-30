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
            return FromArgb(rive_ViewModelInstanceColorRuntime_Value(handle));
        }
        set
        {
            rive_ViewModelInstanceColorRuntime_SetValue(handle, ToArgb(value));
        }
    }

    static Color4 FromArgb(uint argb)
    {
        var a = (byte)((argb >> 24) & 0xFF);
        var r = (byte)((argb >> 16) & 0xFF);
        var g = (byte)((argb >> 8) & 0xFF);
        var b = (byte)(argb & 0xFF);
        return new Color4(new ColorBGRA(r, g, b, a));
    }

    static uint ToArgb(Color4 color)
    {
        color.ToBgra(out byte r, out byte g, out byte b, out byte a);
        return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }
}
