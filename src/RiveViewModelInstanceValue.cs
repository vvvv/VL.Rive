using VL.Rive.Interop;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using VL.Lib.Basics.Imaging;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal unsafe class RiveViewModelInstanceValue : SafeHandle
{
    public static RiveViewModelInstanceValue? FromPropertyData(nint viewModel, RivePropertyData propertyData)
    {
        using var path = new MarshaledString(propertyData.Name);
        switch (propertyData.Type)
        {
            case RiveDataType.String:
                return new RiveViewModelInstanceValue(rive_ViewModelInstanceRuntime_PropertyString(viewModel, path.Value), propertyData);
            case RiveDataType.Number:
                return new RiveViewModelInstanceValue(rive_ViewModelInstanceRuntime_PropertyNumber(viewModel, path.Value), propertyData);
            case RiveDataType.Boolean:
                return new RiveViewModelInstanceValue(rive_ViewModelInstanceRuntime_PropertyBoolean(viewModel, path.Value), propertyData);
            case RiveDataType.Color:
                return new RiveViewModelInstanceValue(rive_ViewModelInstanceRuntime_PropertyColor(viewModel, path.Value), propertyData);
            default:
                return null;
        }
    }

    private readonly RivePropertyData propertyData;

    public RiveViewModelInstanceValue(nint handle, RivePropertyData propertyData)
        : base(default, true)
    {
        SetHandle(handle);
        this.propertyData = propertyData;
    }

    public override bool IsInvalid => handle == default || IsClosed;

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
                    return new Color4(color);
                default:
                    throw new NotSupportedException($"Unsupported data type: {propertyData.Type}");
            }
        }
        set
        {
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
                        rive_ViewModelInstanceColorRuntime_SetValue(handle, (uint)colorValue.ToRgba());
                    }
                    break;
                default:
                    throw new NotSupportedException($"Unsupported data type: {propertyData.Type}");
            }
        }
    }

    protected override bool ReleaseHandle()
    {
        rive_ViewModelInstanceValueRuntime_Destroy(handle);
        return true;
    }
}
