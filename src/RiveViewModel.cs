using System.ComponentModel;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

[Smell(SymbolSmell.Advanced)]
public unsafe class RiveViewModel : IVLObject, INotifyPropertyChanged, IRiveValue<RiveViewModel>
{
    private readonly RiveContext riveContext;
    private readonly nint handle;
    private readonly Dictionary<string, IRiveValue> properties;

    internal RiveViewModel(RiveContext riveContext, nint handle)
    {
        this.riveContext = riveContext;
        this.handle = handle;
        this.properties = CreateProperties();
    }

    // Pointer to RiveViewModelInstance
    internal nint InstanceHandle => rive_ViewModelInstanceRuntime_Instance(handle);

    internal nint Handle => handle;

    internal int ViewModelId => (int)rive_ViewModelInstance_ViewModelId(InstanceHandle);

    internal object? GetObject(string propertyName) => GetRiveValue(propertyName).Value;
    internal void SetObject(string propertyName, object value) => GetRiveValue(propertyName).Value = value;

    private IRiveValue GetRiveValue(string propertyName)
    {
        if (!properties.TryGetValue(propertyName, out var riveValue))
            throw new ArgumentException($"Property '{propertyName}' not found in {Type}.", nameof(propertyName));
        return riveValue;
    }

    internal void AcknowledgeChanges()
    {
        foreach (var (name, property) in properties)
        {
            if (property is RiveValue riveValue && riveValue.HasChanged)
            {
                try
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                }
                finally
                {
                    riveValue.ClearChanges();
                }
            }
            else if (property is RiveViewModel nestedInstance)
            {
                nestedInstance.AcknowledgeChanges();
            }
        }
    }

    // Needs to be public or FromEventPattern crashes
    [Smell(SymbolSmell.Hidden)]
    public event PropertyChangedEventHandler? PropertyChanged;

    internal RiveViewModelType Type => riveContext.ViewModels[ViewModelId];

    AppHost IVLObject.AppHost => riveContext.NodeContext.AppHost;

    NodeContext IVLObject.Context => riveContext.NodeContext;

    IVLTypeInfo IVLObject.Type => Type;

    uint IVLObject.Identity => 0;

    object? IRiveValue.Value
    {
        get => this;
        set => throw new NotImplementedException();
    }

    RiveViewModel? IRiveValue<RiveViewModel>.TypedValue 
    {
        get => this;
        set => throw new NotImplementedException(); 
    }

    IVLObject IVLObject.With(IReadOnlyDictionary<string, object> values)
    {
        foreach (var kvp in values)
        {
            SetObject(kvp.Key, kvp.Value);
        }
        return this;
    }

    object? IVLObject.ReadProperty(string key) => GetObject(key);

    Dictionary<string, IRiveValue> CreateProperties()
    {
        var properties = new Dictionary<string, IRiveValue>(Type.Properties.Length);
        foreach (var p in Type.Properties)
        {
            var runtimeProperty = CreateProperty(handle, p);
            if (runtimeProperty != null)
                properties.Add(p.Name, runtimeProperty);
        }
        return properties;
    }

    IRiveValue? CreateProperty(nint viewModel, RiveViewModelProperty property)
    {
        using var name = new MarshaledString(property.Name);
        var type = property.RiveProperty.Type;
        if (type == RiveDataType.ViewModel)
        {
            var viewModelHandle = rive_ViewModelInstanceRuntime_PropertyViewModel(handle, name);
            if (viewModelHandle != default)
                return new RiveViewModel(riveContext, viewModelHandle);
        }
        else if (type == RiveDataType.List)
        {
            var listHandle = rive_ViewModelInstanceRuntime_PropertyList(handle, name);
            if (listHandle != default)
                return new RiveList(riveContext, listHandle);
        }
        else
        {
            var nativeHandle = rive_ViewModelInstanceRuntime_Property(handle, name);
            if (nativeHandle != default)
                return RiveValue.Create(riveContext, nativeHandle, type);
        }
        return null;
    }
}
