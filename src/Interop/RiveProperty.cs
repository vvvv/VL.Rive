using Stride.Core.Mathematics;
using VL.Lib.Basics.Imaging;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal record struct RiveProperty(string Name, RiveValue Value);

internal struct RiveValue
{
    private readonly nint handle;

    public RiveValue(nint handle)
    {
        if (handle == default)
            throw new ArgumentNullException(nameof(handle));

        this.handle = handle;
        RiveType = rive_ViewModelInstanceValueRuntime_DataType(handle);
    }

    public RiveValue(nint handle, RiveDataType type)
    {
        if (handle == default)
            throw new ArgumentNullException(nameof(handle));

        this.handle = handle;
        RiveType = type;
    }

    public RiveDataType RiveType { get; }

    public Type Type => RiveType switch
    {
        RiveDataType.String => typeof(string),
        RiveDataType.Number => typeof(float),
        RiveDataType.Boolean => typeof(bool),
        RiveDataType.Color => typeof(Color4),
        RiveDataType.EnumType => typeof(Enum), // Placeholder for enum type
        RiveDataType.Trigger => typeof(Action), // Placeholder for trigger type
        RiveDataType.Integer => typeof(int),
        RiveDataType.SymbolListIndex => typeof(int), // Assuming index is an integer
        RiveDataType.AssetImage => typeof(IImage),
        RiveDataType.ViewModel => typeof(RiveViewModelInstance),
        RiveDataType.List => typeof(RiveViewModelList),
        _ => throw new NotSupportedException($"Unsupported data type: {RiveType}")
    };

    public unsafe object Value
    {
        get
        {
            switch (RiveType)
            {
                case RiveDataType.String:
                    return new string(rive_ViewModelInstanceStringRuntime_Value(handle));
                case RiveDataType.Number:
                    return rive_ViewModelInstanceNumberRuntime_Value(handle);
                case RiveDataType.Boolean:
                    return rive_ViewModelInstanceBooleanRuntime_Value(handle) != 0;
                case RiveDataType.Color:
                    var color = rive_ViewModelInstanceColorRuntime_Value(handle);
                    var bgra = new Color4(color);
                    return new Color4(bgra.ToRgba());
                case RiveDataType.Integer:
                    return (int)rive_ViewModelInstanceNumberRuntime_Value(handle);
                case RiveDataType.ViewModel:
                    return new RiveViewModelInstance(handle);
                case RiveDataType.List:
                    return new RiveViewModelList(handle);
                default:
                    throw new NotSupportedException($"Unsupported data type: {RiveType}");
            }
        }
        set
        {
            switch (RiveType)
            {
                case RiveDataType.String:
                    {
                        if (value is not string s)
                            throw new ArgumentException($"Expected a string value.", nameof(value));
                        using var ms = new MarshaledString(s);
                        rive_ViewModelInstanceStringRuntime_SetValue(handle, ms);
                        break;
                    }
                case RiveDataType.Number:
                    if (value is not float f)
                        throw new ArgumentException($"Expected a float value.", nameof(value));
                    rive_ViewModelInstanceNumberRuntime_SetValue(handle, f);
                    break;
                case RiveDataType.Boolean:
                    if (value is not bool b)
                        throw new ArgumentException($"Expected a bool value.", nameof(value));
                    rive_ViewModelInstanceBooleanRuntime_SetValue(handle, (byte)(b ? 1 : 0));
                    break;
                case RiveDataType.Color:
                    if (value is not Color4 c)
                        throw new ArgumentException($"Expected a Color4 value.", nameof(value));
                    rive_ViewModelInstanceColorRuntime_SetValue(handle, (uint)c.ToBgra());
                    break;
                case RiveDataType.Integer:
                    if (value is not int i)
                        throw new ArgumentException($"Expected an int value.", nameof(value));
                    rive_ViewModelInstanceNumberRuntime_SetValue(handle, i);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported data type: {RiveType}");
            }
        }
    }

    public bool? HasChanged
    {
        get
        {
            if (IsRuntimeValue)
                return rive_ViewModelInstanceValueRuntime_HasChanged(handle) != 0;
            return null;
        }
    }

    public void ClearChanges()
    {
        if (IsRuntimeValue)
            rive_ViewModelInstanceValueRuntime_ClearChanges(handle);
    }

    // Some types are not modeled as RuntimeValue internally - maybe we should reflect this in our managed types
    bool IsRuntimeValue => RiveType switch
    {
        RiveDataType.ViewModel => false,
        RiveDataType.SymbolListIndex => false,
        _ => true
    };
}
