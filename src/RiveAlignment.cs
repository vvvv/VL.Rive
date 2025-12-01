using VL.Core;
using VL.Core.Import;

namespace VL.Rive;

[Smell(SymbolSmell.Advanced)]
public enum RiveAlignment
{
    Center,
    CenterLeft,
    CenterRight,
    TopCenter,
    TopLeft,
    TopRight,
    BottomCenter,
    BottomLeft,
    BottomRight
}

static class RiveAlignmentExtensions
{
    public static Interop.RiveAlignment ToNative(this RiveAlignment alignment)
    {
        return alignment switch
        {
            RiveAlignment.TopLeft => Interop.RiveAlignment.topLeft,
            RiveAlignment.TopCenter => Interop.RiveAlignment.topCenter,
            RiveAlignment.TopRight => Interop.RiveAlignment.topRight,
            RiveAlignment.CenterLeft => Interop.RiveAlignment.centerLeft,
            RiveAlignment.Center => Interop.RiveAlignment.center,
            RiveAlignment.CenterRight => Interop.RiveAlignment.centerRight,
            RiveAlignment.BottomLeft => Interop.RiveAlignment.bottomLeft,
            RiveAlignment.BottomCenter => Interop.RiveAlignment.bottomCenter,
            RiveAlignment.BottomRight => Interop.RiveAlignment.bottomRight,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
        };
    }
}
