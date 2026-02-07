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

    public T? GetValue<T>(string propertyName) => GetRiveValueTyped<T>(propertyName).TypedValue;
    public void SetValue<T>(string propertyName, T value) => GetRiveValueTyped<T>(propertyName).TypedValue = value;
    public object? GetObject(string propertyName) => GetRiveValue(propertyName).Value;
    public void SetObject(string propertyName, object value) => GetRiveValue(propertyName).Value = value;

    private IRiveValue GetRiveValue(string propertyName)
    {
        if (!properties.TryGetValue(propertyName, out var riveValue))
            throw new ArgumentException($"Property '{propertyName}' not found in {Type}.", nameof(propertyName));
        return riveValue;
    }

    private IRiveValue<T> GetRiveValueTyped<T>(string propertyName)
    {
        if (!properties.TryGetValue(propertyName, out var value))
            throw new ArgumentException($"Property '{propertyName}' not found in {Type}.", nameof(propertyName));
        if (value is not IRiveValue<T> riveValue)
            throw new InvalidOperationException($"Property '{propertyName}' is not of type {typeof(T).Name}.");
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
        var count = rive_ViewModelInstanceRuntime_PropertyCount(handle);
        var nativeProperties = stackalloc RivePropertyData[count];
        rive_ViewModelInstanceRuntime_Properties(handle, nativeProperties);
        var properties = new Dictionary<string, IRiveValue>(count);
        for (int i = 0; i < count; i++)
        {
            var rivePropertyData = nativeProperties[i];
            try
            {
                var name = Interop.SpanExtensions.AsString(rivePropertyData.name);

                // Skip properties with slashes in their names - Rive treats these as paths and will not be able to resolve them correctly
                if (name.Contains('/'))
                    continue;

                var property = CreateProperty(handle, rivePropertyData);
                if (property is null)
                    continue;

                properties.Add(name, property);
            }
            finally
            {
                NativeMemory.Free(rivePropertyData.name); // Free the native string memory
            }
        }
        return properties;
    }

    IRiveValue? CreateProperty(nint viewModel, RivePropertyData property)
    {
        var type = (RiveDataType)property.type;
        if (type == RiveDataType.ViewModel)
        {
            var viewModelHandle = rive_ViewModelInstanceRuntime_PropertyViewModel(handle, property.name);
            if (viewModelHandle != default)
                return new RiveViewModel(riveContext, viewModelHandle);
        }
        else if (type == RiveDataType.List)
        {
            var listHandle = rive_ViewModelInstanceRuntime_PropertyList(handle, property.name);
            if (listHandle != default)
                return new RiveList(riveContext, listHandle);
        }
        else
        {
            var nativeHandle = rive_ViewModelInstanceRuntime_Property(handle, property.name);
            if (nativeHandle != default)
                return RiveValue.Create(riveContext, nativeHandle, (RiveDataType)property.type);
        }
        return null;
    }
}
