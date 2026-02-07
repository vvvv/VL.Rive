using VL.Core;
using VL.Lib.Collections;
using VL.Rive.Interop;

namespace VL.Rive.Internal;

internal sealed class RiveViewModelType : IVLTypeInfo
{
    private readonly RiveContext riveContext;
    private readonly nint handle;
    private readonly RiveViewModelInfo viewModel;
    private readonly Spread<IVLPropertyInfo> properties;

    internal RiveViewModelType(RiveContext riveContext, nint handle, RiveViewModelInfo viewModel)
    {
        this.riveContext = riveContext;
        this.handle = handle;
        this.viewModel = viewModel;
        this.properties = viewModel.Properties.Select(p => new RiveViewModelProperty(this.riveContext, this, p)).ToSpread<IVLPropertyInfo>();
    }

    public RiveViewModel CreateInstance()
    {
        var instanceHandle = Methods.rive_ViewModelRuntime_CreateInstance(handle);
        return new RiveViewModel(riveContext, instanceHandle);
    }

    public RiveViewModel CreateDefaultInstance()
    {
        var instanceHandle = Methods.rive_ViewModelRuntime_CreateDefaultInstance(handle);
        return new RiveViewModel(riveContext, instanceHandle);
    }

    public override string ToString() => self.Name;

    IVLTypeInfo self => this;

    string IVLTypeInfo.Name => viewModel.Name;

    string IVLTypeInfo.Category => "Rive";

    string IVLTypeInfo.FullName => $"{self.Category}.{self.Name}";

    UniqueId IVLTypeInfo.Id => default;

    Type IVLTypeInfo.ClrType => typeof(RiveViewModelType);

    bool IVLTypeInfo.IsPatched => false;

    bool IVLTypeInfo.IsClass => true;

    bool IVLTypeInfo.IsRecord => false;

    bool IVLTypeInfo.IsImmutable => false;

    bool IVLTypeInfo.IsInterface => false;

    Spread<IVLPropertyInfo> IVLTypeInfo.Properties => properties;

    Spread<IVLPropertyInfo> IVLTypeInfo.AllProperties => self.Properties;

    Spread<Attribute> IHasAttributes.Attributes => Spread<Attribute>.Empty;

    object? IVLTypeInfo.CreateInstance(NodeContext context, IReadOnlyDictionary<string, object?>? arguments)
    {
        return CreateInstance();
    }

    object? IVLTypeInfo.GetDefaultValue()
    {
        return CreateDefaultInstance();
    }

    IVLPropertyInfo? IVLTypeInfo.GetProperty(string name)
    {
        return properties.FirstOrDefault(p => p.OriginalName == name);
    }

    IVLTypeInfo IVLTypeInfo.MakeGenericType(IReadOnlyList<IVLTypeInfo> arguments)
    {
        throw new NotSupportedException();
    }

    string IVLTypeInfo.ToString(bool includeCategory) => includeCategory ? self.FullName : self.Name;
}
