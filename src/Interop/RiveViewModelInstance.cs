using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal unsafe class RiveViewModelInstance
{
    private readonly nint handle;
    private ImmutableArray<RiveProperty> properties;

    public RiveViewModelInstance(nint handle)
    {
        if (handle == default)
            throw new ArgumentNullException(nameof(handle));
        this.handle = handle;
    }

    // Pointer to RiveViewModelInstance
    internal nint InstanceHandle => rive_ViewModelInstanceRuntime_Instance(handle);

    internal nint Handle => handle;

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
                    var propertyData = new PropertyData(SpanExtensions.AsString(nativeProperty.name), (RiveDataType)nativeProperty.type);
                    NativeMemory.Free(nativeProperty.name); // Free the native string memory

                    var property = FromPropertyData(handle, propertyData);
                    if (property is null)
                        continue;

                    properties.Add(property.Value);
                }
                this.properties = properties.ToImmutable();
            }
            return properties;

            RiveProperty? FromPropertyData(nint viewModel, PropertyData propertyData)
            {
                if (propertyData.Name.Contains('/'))
                    return null; // Skip properties with slashes in their names - Rive treats these as paths and will not be able to resolve them correctly

                // rive_ViewModelInstanceRuntime_Property does not handle ViewModel properties correctly, so we need to call the specific functions based on the property type
                using var path = new MarshaledString(propertyData.Name);
                var valueHandle = propertyData.Type switch
                {
                    RiveDataType.String => rive_ViewModelInstanceRuntime_PropertyString(handle, path.Value),
                    RiveDataType.Number or RiveDataType.Integer => rive_ViewModelInstanceRuntime_PropertyNumber(handle, path.Value),
                    RiveDataType.Boolean => rive_ViewModelInstanceRuntime_PropertyBoolean(handle, path.Value),
                    RiveDataType.Color => rive_ViewModelInstanceRuntime_PropertyColor(handle, path.Value),
                    RiveDataType.List => rive_ViewModelInstanceRuntime_PropertyList(handle, path.Value),
                    RiveDataType.EnumType => rive_ViewModelInstanceRuntime_PropertyEnum(handle, path.Value),
                    RiveDataType.Trigger => rive_ViewModelInstanceRuntime_PropertyTrigger(handle, path.Value),
                    RiveDataType.ViewModel => rive_ViewModelInstanceRuntime_PropertyViewModel(handle, path.Value),
                    //RiveDataType.Integer => rive_ViewModelInstanceRuntime_PropertyInteger(handle, path.Value),
                    //RiveDataType.SymbolListIndex => rive_ViewModelInstanceRuntime_PropertySymbolListIndex(handle, path.Value),
                    //RiveDataType.AssetImage => rive_ViewModelInstanceRuntime_PropertyAssetImage(handle, path.Value),
                    //RiveDataType.Artboard => rive_ViewModelInstanceRuntime_PropertyArtboard(handle, path.Value),
                    _ => 0,
                };

                if (valueHandle != default)
                {
                    var riveValue = new RiveValue(valueHandle, propertyData.Type);
                    return new RiveProperty(propertyData.Name, riveValue);
                }

                return null;
            }
        }
    }
}
