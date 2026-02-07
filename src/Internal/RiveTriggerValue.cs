using System.Reactive;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal sealed class RiveTriggerValue : RiveValue<Unit>
{
    public RiveTriggerValue(nint handle) : base(handle, RiveDataType.Trigger) { }

    public override Unit TypedValue
    {
        get => default;
        set => rive_ViewModelInstanceTriggerRuntime_Trigger(handle);
    }
}
