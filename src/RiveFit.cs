using VL.Core;
using VL.Core.Import;

namespace VL.Rive;

[Smell(SymbolSmell.Advanced)]
public enum RiveFit
{
    Contain,
    FitWidth,
    FitHeight,
    Cover,
    Fill,
    Layout,
    None,
    ScaleDown,
}

static class RiveFitExtensions
{
    public static Interop.RiveFit ToNative(this RiveFit fit)
    {
        return fit switch
        {
            RiveFit.Fill => Interop.RiveFit.Fill,
            RiveFit.Contain => Interop.RiveFit.Contain,
            RiveFit.Cover => Interop.RiveFit.Cover,
            RiveFit.FitWidth => Interop.RiveFit.FitWidth,
            RiveFit.FitHeight => Interop.RiveFit.FitHeight,
            RiveFit.Layout => Interop.RiveFit.Layout,
            RiveFit.None => Interop.RiveFit.None,
            RiveFit.ScaleDown => Interop.RiveFit.ScaleDown,
            _ => throw new ArgumentOutOfRangeException(nameof(fit), fit, null),
        };
    }
}