using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal abstract class RiveValue : IRiveValue
{
    protected readonly nint handle;

    protected RiveValue(nint handle, RiveDataType type)
    {
        this.handle = handle;
        RiveType = type;
    }

    public RiveDataType RiveType { get; }

    public abstract Type Type { get; }

    public abstract object? Value { get; set; }

    public bool HasChanged => rive_ViewModelInstanceValueRuntime_HasChanged(handle) != 0;

    public void ClearChanges() => rive_ViewModelInstanceValueRuntime_ClearChanges(handle);

    public static RiveValue Create(RiveContext context, nint handle, RiveDataType type)
    {
        return type switch
        {
            RiveDataType.String => new RiveStringValue(handle),
            RiveDataType.Number => new RiveNumberValue(handle),
            RiveDataType.Boolean => new RiveBooleanValue(handle),
            RiveDataType.Color => new RiveColorValue(handle),
            RiveDataType.EnumType => new RiveEnumValue(handle),
            RiveDataType.Trigger => new RiveTriggerValue(handle),
            RiveDataType.AssetImage => new RiveImageValue(handle, context),
            RiveDataType.Artboard => new RiveArtboardValue(handle, context),
            _ => throw new ArgumentException($"Unsupported RiveDataType: {type}", nameof(type))
        };
    }
}

internal abstract class RiveValue<T> : RiveValue, IRiveValue<T>
{
    protected RiveValue(nint handle, RiveDataType type) : base(handle, type)
    {
    }

    public override Type Type => typeof(T);

    public abstract T? TypedValue { get; set; }

    public override object? Value
    {
        get => TypedValue;
        set => TypedValue = (T?)value;
    }
}
