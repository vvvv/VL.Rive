using Stride.Core.Mathematics;
using System.Reactive;
using VL.Core;
using VL.Lib.Collections;
using VL.Rive.Interop;

namespace VL.Rive.Internal;

internal sealed class RiveViewModelProperty : IVLPropertyInfo
{
    private readonly RiveContext riveContext;
    private readonly TypeRegistry typeRegistry;
    private readonly RiveViewModelType viewModel;
    private readonly RivePropertyInfo riveProperty;

    internal RiveViewModelProperty(RiveContext riveContext, RiveViewModelType viewModel, RivePropertyInfo riveProperty)
    {
        this.riveContext = riveContext;
        this.typeRegistry = riveContext.TypeRegistry;
        this.viewModel = viewModel;
        this.riveProperty = riveProperty;
    }

    public override string ToString() => riveProperty.Name;

    IVLTypeInfo IVLPropertyInfo.DeclaringType => viewModel;

    string IVLPropertyInfo.Name => riveProperty.Name;

    string IVLPropertyInfo.NameForTextualCode => riveProperty.Name;

    string IVLPropertyInfo.OriginalName => riveProperty.Name;

    IVLTypeInfo IVLPropertyInfo.Type => riveProperty.Type switch
    {
        RiveDataType.String => typeRegistry.GetTypeInfo(typeof(string)),
        RiveDataType.Number => typeRegistry.GetTypeInfo(typeof(float)),
        RiveDataType.Boolean => typeRegistry.GetTypeInfo(typeof(bool)),
        RiveDataType.Color => typeRegistry.GetTypeInfo(typeof(Color4)),
        RiveDataType.Trigger => typeRegistry.GetTypeInfo(typeof(Unit)),
        RiveDataType.ViewModel => riveContext.ViewModels[riveProperty.ViewModelReferenceId],
        RiveDataType.List => typeRegistry.GetTypeInfo(typeof(RiveList)),
        _ => typeRegistry.GetTypeInfo(typeof(object))
    };

    bool IVLPropertyInfo.IsManaged => false;

    bool IVLPropertyInfo.ShouldBeSerialized => false;

    Spread<Attribute> IHasAttributes.Attributes => Spread<Attribute>.Empty;

    object? IVLPropertyInfo.GetValue(object instance)
    {
        var viewModelInstance = (RiveViewModel)instance;
        return viewModelInstance.GetObject(riveProperty.Name);
    }

    object IVLPropertyInfo.WithValue(object instance, object? value)
    {
        var viewModelInstance = (RiveViewModel)instance;
        viewModelInstance.SetObject(riveProperty.Name, value!);
        return viewModelInstance;
    }
}
