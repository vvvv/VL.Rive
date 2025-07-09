using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using VL.Lib.Basics.Imaging;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

// Maps to RiveViewModelInstanceValueRuntime in the C++ code
internal unsafe class RiveViewModelInstanceValue : RiveProperty
{
    private readonly RiveViewModelInstance parent;
    private readonly PropertyData propertyData;

    public RiveViewModelInstanceValue(RiveViewModelInstance parent, nint handle, PropertyData propertyData)
        : base(propertyData.Name, GetType(propertyData.Type), handle, true)
    {
        if (handle == nint.Zero)
            throw new ArgumentNullException(nameof(handle), "Handle cannot be zero.");

        this.parent = parent;
        this.propertyData = propertyData;
    }

    public override bool IsInvalid => parent.IsInvalid;

    private static Type GetType(RiveDataType riveDataType) => riveDataType switch
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
        RiveDataType.ViewModel => typeof(RiveViewModelInstance),
        _ => throw new NotSupportedException($"Unsupported data type: {riveDataType}")
    };

    public bool HasChanged
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsInvalid, "RiveViewModelInstance is disposed.");
            return rive_ViewModelInstanceValueRuntime_HasChanged(handle) != 0;
        }
    }

    public void ClearChanges()
    {
        ObjectDisposedException.ThrowIf(IsInvalid, "RiveViewModelInstance is disposed.");
        rive_ViewModelInstanceValueRuntime_ClearChanges(handle);
    }

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
