using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

internal unsafe class RiveViewModelInstance : SafeHandle
{
    private ImmutableArray<RiveViewModelInstanceValue> properties;

    public RiveViewModelInstance(nint handle, bool ownsHandle = true)
        : base(default, ownsHandle)
    {
        SetHandle(handle);
    }

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

                    var property = RiveViewModelInstanceValue.FromPropertyData(handle, propertyData);
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
