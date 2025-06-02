using RiveSharpInterop;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Basics.Imaging;
using VL.OpenEXR;
using static RiveSharpInterop.Methods;

namespace VL.Rive
{
    enum DataType : uint
    {
        None = 0,
        String = 1,
        Number = 2,
        Boolean = 3,
        Color = 4,
        List = 5,
        EnumType = 6,
        Trigger = 7,
        ViewModel = 8,
        Integer = 9,
        SymbolListIndex = 10,
        AssetImage = 11
    }

    record struct PropertyData(string Name, DataType Type);

    internal unsafe class ViewModelInstance : SafeHandle
    {
        private ImmutableArray<ViewModelValue> properties;

        public ViewModelInstance(nint handle, bool ownsHandle = true)
            : base(default, ownsHandle)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == default || IsClosed;

        public ImmutableArray<ViewModelValue> Properties
        {
            get
            {
                if (properties.IsDefault)
                {
                    var count = rive_ViewModelInstanceRuntime_PropertyCount(handle);
                    var nativeProperties = stackalloc RivePropertyData[count];
                    rive_ViewModelInstanceRuntime_Properties(handle, nativeProperties);
                    var properties = ImmutableArray.CreateBuilder<ViewModelValue>(count);
                    for (int i = 0; i < count; i++)
                    {
                        var nativeProperty = nativeProperties[i];
                        var propertyData = new PropertyData(Marshal.PtrToStringAnsi((nint)nativeProperty.name) ?? string.Empty, (DataType)nativeProperty.type);
                        NativeMemory.Free(nativeProperty.name); // Free the native string memory

                        var property = ViewModelValue.FromPropertyData(handle, propertyData);
                        if (property is null)
                            continue;

                        properties.Add(property);
                    }
                    this.properties = properties.ToImmutable();
                }
                return properties;
            }
        }

        protected override bool ReleaseHandle()
        {
            rive_ViewModelInstanceRuntime_Destroy(handle);
            return true;
        }
    }

    internal unsafe class ViewModelValue : SafeHandle
    {
        public static ViewModelValue? FromPropertyData(nint viewModel, PropertyData propertyData)
        {
            using var path = new MarshaledString(propertyData.Name);
            switch (propertyData.Type)
            {
                case DataType.String:
                    return new ViewModelValue(rive_ViewModelInstanceRuntime_PropertyString(viewModel, path.Value), propertyData);
                case DataType.Number:
                    return new ViewModelValue(rive_ViewModelInstanceRuntime_PropertyNumber(viewModel, path.Value), propertyData);
                case DataType.Boolean:
                    return new ViewModelValue(rive_ViewModelInstanceRuntime_PropertyBoolean(viewModel, path.Value), propertyData);
                case DataType.Color:
                    return new ViewModelValue(rive_ViewModelInstanceRuntime_PropertyColor(viewModel, path.Value), propertyData);
                default:
                    return null;
            }
        }

        private readonly PropertyData propertyData;

        public ViewModelValue(nint handle, PropertyData propertyData)
            : base(default, true)
        {
            SetHandle(handle);
            this.propertyData = propertyData;
        }

        public override bool IsInvalid => handle == default || IsClosed;

        public string Name => propertyData.Name;

        public Type Type => propertyData.Type switch
        {
            DataType.String => typeof(string),
            DataType.Number => typeof(float),
            DataType.Boolean => typeof(bool),
            DataType.Color => typeof(Color4),
            DataType.List => typeof(List<object>), // Placeholder for list type
            DataType.EnumType => typeof(Enum), // Placeholder for enum type
            DataType.Trigger => typeof(Action), // Placeholder for trigger type
            DataType.Integer => typeof(int),
            DataType.SymbolListIndex => typeof(int), // Assuming index is an integer
            DataType.AssetImage => typeof(IImage),
            _ => throw new NotSupportedException($"Unsupported data type: {propertyData.Type}")
        };

        public object Value
        {
            get
            {
                switch (propertyData.Type)
                {
                    case DataType.String:
                        return Marshal.PtrToStringAnsi((nint)rive_ViewModelInstanceStringRuntime_Value(handle)) ?? string.Empty;
                    case DataType.Number:
                        return rive_ViewModelInstanceNumberRuntime_Value(handle);
                    case DataType.Boolean:
                        return rive_ViewModelInstanceBooleanRuntime_Value(handle) != 0;
                    case DataType.Color:
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
                    case DataType.String:
                        if (value is string strValue)
                        {
                            rive_ViewModelInstanceStringRuntime_SetValue(handle, (sbyte*)Marshal.StringToHGlobalAnsi(strValue));
                        }
                        break;
                    case DataType.Number:
                        if (value is float floatValue)
                        {
                            rive_ViewModelInstanceNumberRuntime_SetValue(handle, floatValue);
                        }
                        break;
                    case DataType.Boolean:
                        if (value is bool boolValue)
                        {
                            rive_ViewModelInstanceBooleanRuntime_SetValue(handle, boolValue ? (byte)1 : (byte)0);
                        }
                        break;
                    case DataType.Color:
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
}
