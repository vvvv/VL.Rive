using System.Collections.Immutable;
using System.Runtime.InteropServices;
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
        Properties = viewModel.Properties.Select(p => new RiveViewModelProperty(this.riveContext, this, p)).ToImmutableArray();
        this.properties = Properties.ToSpread<IVLPropertyInfo>();
        InstanceNames = UniqueInstanceNames(ReadInstanceNames(handle));
    }

    private static unsafe List<string?> ReadInstanceNames(nint handle)
    {
        var count = Methods.rive_ViewModelRuntime_InstanceCount(handle);
        var names = new List<string?>(count);
        for (var i = 0; i < count; i++)
        {
            var namePtr = Methods.rive_ViewModelRuntime_InstanceNameAt(handle, i);
            if (namePtr == null)
            {
                names.Add(null);
                continue;
            }
            try
            {
                names.Add(Interop.SpanExtensions.AsString(namePtr));
            }
            finally
            {
                NativeMemory.Free(namePtr);
            }
        }
        return names;
    }

    /// <summary>
    /// The instances of a view model can have no name or the same one. This gives each a name of its own to choose it
    /// by: the names there are, the second of the same one with a number behind it, and "Instance" (and the numbers behind
    /// it) for the ones that have none. Kept in the order of the instances.
    /// </summary>
    internal static ImmutableArray<string> UniqueInstanceNames(IReadOnlyList<string?> names)
    {
        var result = new string[names.Count];
        var used = new HashSet<string>();

        // The names there are keep their name, so a name that is made up never takes one of them
        for (var i = 0; i < names.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(names[i]))
                result[i] = Unique(names[i]!, used);
        }
        for (var i = 0; i < names.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(names[i]))
                result[i] = Unique("Instance", used);
        }
        return ImmutableCollectionsMarshal.AsImmutableArray(result);

        static string Unique(string name, HashSet<string> used)
        {
            var candidate = name;
            for (var number = 2; !used.Add(candidate); number++)
                candidate = $"{name} {number}";
            return candidate;
        }
    }

    /// <summary>
    /// The names of the instances of this view model that are defined in the file, as in the Rive editor, with a name
    /// made up for the ones that have none (see <see cref="UniqueInstanceNames"/>). The first one is the default instance.
    /// </summary>
    internal ImmutableArray<string> InstanceNames { get; }

    /// <summary>A new instance that is a copy of the instance of the given name, null if the view model has none of that name.</summary>
    public RiveViewModel? CreateInstanceFromName(string instanceName)
    {
        var index = InstanceNames.IndexOf(instanceName);
        if (index < 0)
            return null;

        // By index: another instance could have the same name in the file
        var instanceHandle = Methods.rive_ViewModelRuntime_CreateInstanceFromIndex(handle, index);
        return instanceHandle == default ? null : riveContext.WrapViewModel(instanceHandle);
    }

    public RiveViewModel CreateInstance()
    {
        var instanceHandle = Methods.rive_ViewModelRuntime_CreateInstance(handle);
        return riveContext.WrapViewModel(instanceHandle);
    }

    public RiveViewModel CreateDefaultInstance()
    {
        var instanceHandle = Methods.rive_ViewModelRuntime_CreateDefaultInstance(handle);
        return riveContext.WrapViewModel(instanceHandle);
    }

    public override string ToString() => self.Name;

    internal string Name => viewModel.Name;

    internal ImmutableArray<RiveViewModelProperty> Properties { get; }

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
