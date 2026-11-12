using System.ComponentModel;
using System.Runtime.InteropServices;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;
using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

/// <summary>
/// An instance of a Rive view model. It owns one reference to the native instance, which it releases when disposed
/// or - for instances nobody disposes, such as those created in a patch - when it is finalized. An instance that is
/// an item of a <see cref="RiveList"/> or bound to a scene is kept alive natively by the list or the scene for as
/// long as it is used there, whatever happens to this object.
/// </summary>
[Smell(SymbolSmell.Advanced)]
public unsafe class RiveViewModel : IVLObject, INotifyPropertyChanged, IRiveValue<RiveViewModel>, IDisposable
{
    private readonly RiveContext riveContext;
    private readonly nint handle;
    private readonly nint identity;
    private readonly Dictionary<string, IRiveValue> properties;
    private readonly string typeName;
    private int disposed;

    /// <param name="handle">A native ViewModelInstanceRuntime. This view model takes over the reference the caller holds.</param>
    internal RiveViewModel(RiveContext riveContext, nint handle)
    {
        this.riveContext = riveContext;
        this.handle = handle;
        this.identity = rive_ViewModelInstanceRuntime_Instance(handle);
        this.properties = CreateProperties();
        this.typeName = Type.Name;
    }

    ~RiveViewModel() => Release();

    void IDisposable.Dispose() => Dispose();

    /// <summary>Releases the native instance. Not needed for correctness, the finalizer does it as well.</summary>
    internal void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    private void Release()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        riveContext.ForgetViewModel(identity, this);
        rive_ViewModelInstanceRuntime_Release(handle);
    }

    internal bool IsDisposed => Volatile.Read(ref disposed) != 0;

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    // Pointer to RiveViewModelInstance, borrowed from the native instance
    internal nint InstanceHandle
    {
        get
        {
            ThrowIfDisposed();
            return identity;
        }
    }

    // Identifies the native instance for as long as this object exists, even when it is disposed
    internal nint Identity => identity;

    internal nint Handle
    {
        get
        {
            ThrowIfDisposed();
            return handle;
        }
    }

    internal int ViewModelId => (int)rive_ViewModelInstance_ViewModelId(InstanceHandle);

    internal object? GetObject(string propertyName)
    {
        ThrowIfDisposed();
        return GetRiveValue(propertyName).Value;
    }

    internal void SetObject(string propertyName, object value)
    {
        ThrowIfDisposed();
        var current = GetRiveValue(propertyName);

        // A nested view model is replaced on the instance that holds it - the nested object cannot do that itself
        if (current is RiveViewModel && value is RiveViewModel replacement)
        {
            if (!ReferenceEquals(current, replacement))
                ReplaceViewModel(propertyName, replacement);
            return;
        }

        current.Value = value;
    }

    private void ReplaceViewModel(string propertyName, RiveViewModel replacement)
    {
        using var name = new MarshaledString(propertyName);
        if (rive_ViewModelInstanceRuntime_ReplaceViewModel(handle, name, replacement.Handle) == 0)
            throw new ArgumentException($"Cannot assign the view model instance to '{propertyName}', it is not of the type the property expects.", nameof(replacement));
        properties[propertyName] = replacement;
    }

    private IRiveValue GetRiveValue(string propertyName)
    {
        if (!properties.TryGetValue(propertyName, out var riveValue))
            throw new ArgumentException($"Property '{propertyName}' not found in {Type}.", nameof(propertyName));
        return riveValue;
    }

    internal void AcknowledgeChanges()
    {
        if (IsDisposed)
            return;

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

    [Smell(SymbolSmell.Hidden)]
    public override string ToString() => IsDisposed ? $"{typeName} (disposed)" : typeName;

    AppHost IVLObject.AppHost => riveContext.NodeContext.AppHost;

    NodeContext IVLObject.Context => riveContext.NodeContext;

    IVLTypeInfo IVLObject.Type => Type;

    uint IVLObject.Identity => 0;

    object? IRiveValue.Value
    {
        get => this;
        set => AssignFromParent(value);
    }

    // ByPath nodes like to write the same instance back - let's allow that. Replacing it is done on the parent
    // instance (see SetObject), where the property lives.
    private void AssignFromParent(object? value)
    {
        if (!ReferenceEquals(value, this))
            throw new InvalidOperationException("A view model instance can only be replaced through the property of the instance that holds it.");
    }

    RiveViewModel? IRiveValue<RiveViewModel>.TypedValue
    {
        get => this;
        set => AssignFromParent(value);
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
                return riveContext.WrapViewModel(viewModelHandle);
        }
        else if (type == RiveDataType.List)
        {
            var listHandle = rive_ViewModelInstanceRuntime_PropertyList(handle, name);
            if (listHandle != default)
                return new RiveList(riveContext, this, listHandle);
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
