using VL.Rive.Interop;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using VL.Lib.Basics.Imaging;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

// Maps to RiveViewModelInstanceValueRuntime in the C++ code
internal unsafe class RiveViewModelInstanceValue : SafeHandle
{
    private readonly RiveViewModelInstance parent;
    private readonly RivePropertyData propertyData;

    public RiveViewModelInstanceValue(RiveViewModelInstance parent, nint handle, RivePropertyData propertyData)
        : base(default, true)
    {
        this.parent = parent;
        this.propertyData = propertyData;
        SetHandle(handle);
    }

    public override bool IsInvalid => parent.IsInvalid;

    public string Name => propertyData.Name;

    public Type Type => propertyData.Type switch
    {
        RiveDataType.String => typeof(string),
        RiveDataType.Number => typeof(float),
        RiveDataType.Boolean => typeof(bool),
        RiveDataType.Color => typeof(Color4),
        RiveDataType.List => typeof(List<object>), // Placeholder for list type
        RiveDataType.EnumType => typeof(Enum), // Placeholder for enum type
        RiveDataType.Trigger => typeof(Action), // Placeholder for trigger type
        RiveDataType.Integer => typeof(int),
        RiveDataType.SymbolListIndex => typeof(int), // Assuming index is an integer
        RiveDataType.AssetImage => typeof(IImage),
        _ => throw new NotSupportedException($"Unsupported data type: {propertyData.Type}")
    };

    public object Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsInvalid, "RiveViewModelInstance is disposed.");

            switch (propertyData.Type)
            {
                case RiveDataType.String:
                    return Marshal.PtrToStringAnsi((nint)rive_ViewModelInstanceStringRuntime_Value(handle)) ?? string.Empty;
                case RiveDataType.Number:
                    return rive_ViewModelInstanceNumberRuntime_Value(handle);
                case RiveDataType.Boolean:
                    return rive_ViewModelInstanceBooleanRuntime_Value(handle) != 0;
                case RiveDataType.Color:
                    var color = rive_ViewModelInstanceColorRuntime_Value(handle);
                    var bgra = new Color4(color);
                    return new Color4(bgra.ToRgba());
                default:
                    throw new NotSupportedException($"Unsupported data type: {propertyData.Type}");
            }
        }
        set
        {
            ObjectDisposedException.ThrowIf(IsInvalid, "RiveViewModelInstance is disposed.");

            switch (propertyData.Type)
            {
                case RiveDataType.String:
                    if (value is string strValue)
                    {
                        rive_ViewModelInstanceStringRuntime_SetValue(handle, (sbyte*)Marshal.StringToHGlobalAnsi(strValue));
                    }
                    break;
                case RiveDataType.Number:
                    if (value is float floatValue)
                    {
                        rive_ViewModelInstanceNumberRuntime_SetValue(handle, floatValue);
                    }
                    break;
                case RiveDataType.Boolean:
                    if (value is bool boolValue)
                    {
                        rive_ViewModelInstanceBooleanRuntime_SetValue(handle, boolValue ? (byte)1 : (byte)0);
                    }
                    break;
                case RiveDataType.Color:
                    if (value is Color4 colorValue)
                    {
                        rive_ViewModelInstanceColorRuntime_SetValue(handle, (uint)colorValue.ToBgra());
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unsupported data type: {propertyData.Type}");
            }
        }
    }

    protected override bool ReleaseHandle()
    {
        // Cleaned up by the parent instance
        return true;
    }
}
