using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal unsafe class RiveViewModelInstance : RiveProperty
{
    private ImmutableArray<RiveProperty> properties;

    public RiveViewModelInstance(string name, nint handle)
        : base(name, typeof(RiveViewModelInstance), handle)
    {
    }

    // Pointer to RiveViewModelInstance
    internal nint InstanceHandle => rive_ViewModelInstanceRuntime_Instance(handle);

    public ImmutableArray<RiveProperty> Properties
    {
        get
        {
            if (properties.IsDefault)
            {
                var count = rive_ViewModelInstanceRuntime_PropertyCount(handle);
                var nativeProperties = stackalloc RivePropertyData[count];
                rive_ViewModelInstanceRuntime_Properties(handle, nativeProperties);
                var properties = ImmutableArray.CreateBuilder<RiveProperty>(count);
                for (int i = 0; i < count; i++)
                {
                    var nativeProperty = nativeProperties[i];
                    var propertyData = new PropertyData(Marshal.PtrToStringAnsi((nint)nativeProperty.name) ?? string.Empty, (RiveDataType)nativeProperty.type);
                    NativeMemory.Free(nativeProperty.name); // Free the native string memory

                    var property = FromPropertyData(handle, propertyData);
                    if (property is null)
                        continue;

                    properties.Add(property);
                }
                this.properties = properties.ToImmutable();
            }
            return properties;

            RiveProperty? FromPropertyData(nint viewModel, PropertyData propertyData)
            {
                if (propertyData.Name.Contains('/'))
                    return null; // Skip properties with slashes in their names - Rive treats these as paths and will not be able to resolve them correctly

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
                    case RiveDataType.ViewModel:
                        return new RiveViewModelInstance(propertyData.Name, rive_ViewModelInstanceRuntime_PropertyViewModel(viewModel, path.Value));
                    default:
                        return null;
                }
            }
        }
    }
}
