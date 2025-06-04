using System.Collections.Immutable;
using System.Runtime.InteropServices;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

// Maps to the RiveViewModelInstanceRuntime class in the Rive C++ runtime
internal unsafe class RiveViewModelInstance : SafeHandle
{
    private ImmutableArray<RiveViewModelInstanceValue> properties;

    public RiveViewModelInstance(nint handle, bool ownsHandle = true)
        : base(default, ownsHandle)
    {
        SetHandle(handle);
    }

    // Pointer to RiveViewModelInstance
    internal nint InstanceHandle => rive_ViewModelInstanceRuntime_Instance(handle);

    public override bool IsInvalid => handle == default || IsClosed;

    public ImmutableArray<RiveViewModelInstanceValue> Properties
    {
        get
        {
            if (properties.IsDefault)
            {
                var count = rive_ViewModelInstanceRuntime_PropertyCount(handle);
                var nativeProperties = stackalloc Interop.RivePropertyData[count];
                rive_ViewModelInstanceRuntime_Properties(handle, nativeProperties);
                var properties = ImmutableArray.CreateBuilder<RiveViewModelInstanceValue>(count);
                for (int i = 0; i < count; i++)
                {
                    var nativeProperty = nativeProperties[i];
                    var propertyData = new RivePropertyData(Marshal.PtrToStringAnsi((nint)nativeProperty.name) ?? string.Empty, (RiveDataType)nativeProperty.type);
                    NativeMemory.Free(nativeProperty.name); // Free the native string memory

                    var property = FromPropertyData(handle, propertyData);
                    if (property is null)
                        continue;

                    properties.Add(property);
                }
                this.properties = properties.ToImmutable();
            }
            return properties;

            RiveViewModelInstanceValue? FromPropertyData(nint viewModel, RivePropertyData propertyData)
            {
                using var path = new MarshaledString(propertyData.Name);
                switch (propertyData.Type)
                {
                    case RiveDataType.String:
                        return new RiveViewModelInstanceValue(this, rive_ViewModelInstanceRuntime_PropertyString(viewModel, path.Value), propertyData);
                    case RiveDataType.Number:
                        return new RiveViewModelInstanceValue(this, rive_ViewModelInstanceRuntime_PropertyNumber(viewModel, path.Value), propertyData);
                    case RiveDataType.Boolean:
                        return new RiveViewModelInstanceValue(this, rive_ViewModelInstanceRuntime_PropertyBoolean(viewModel, path.Value), propertyData);
                    case RiveDataType.Color:
                        return new RiveViewModelInstanceValue(this, rive_ViewModelInstanceRuntime_PropertyColor(viewModel, path.Value), propertyData);
                    default:
                        return null;
                }
            }
        }
    }

    protected override bool ReleaseHandle()
    {
        rive_ViewModelInstanceRuntime_Destroy(handle);
        return true;
    }
}
