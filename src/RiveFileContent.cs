using VL.Lib.Collections;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>What a Rive file holds.</summary>
/// <param name="Artboards">The artboards, with their state machines and animations</param>
/// <param name="ViewModels">The view models, with their properties</param>
[Smell(SymbolSmell.Advanced)]
public sealed record RiveFileContent(Spread<RiveArtboardContent> Artboards, Spread<RiveViewModelContent> ViewModels)
{
    internal static RiveFileContent From(RiveFile file) => new(
        file.Artboards.Select(a => new RiveArtboardContent(
            a.Name,
            a.StateMachines.Select(s => new RiveStateMachineContent(s.Name)).ToSpread(),
            a.Animations.Select(x => new RiveAnimationContent(x.Name)).ToSpread(),
            a.DefaultViewModel)).ToSpread(),
        file.ViewModels.Select(v => new RiveViewModelContent(
            v.Name,
            v.Properties.Select(p => new RivePropertyContent(p.Name, (RivePropertyKind)(int)p.Type, p.ViewModelReferenceId)).ToSpread())).ToSpread());

    public override string ToString() => $"{Artboards.Count} artboards, {ViewModels.Count} view models";
}

/// <summary>An artboard of a Rive file: its state machines, animations and the view model it is made for.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveArtboardContent(
    string Name,
    Spread<RiveStateMachineContent> StateMachines,
    Spread<RiveAnimationContent> Animations,
    string? DefaultViewModel)
{
    public override string ToString()
        => $"{Name} ({StateMachines.Count} state machines, {Animations.Count} animations"
            + (DefaultViewModel is null ? ")" : $", view model {DefaultViewModel})");
}

/// <summary>A state machine of an artboard.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveStateMachineContent(string Name)
{
    public override string ToString() => Name;
}

/// <summary>An animation of an artboard.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveAnimationContent(string Name)
{
    public override string ToString() => Name;
}

/// <summary>A view model of a Rive file and its properties.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveViewModelContent(string Name, Spread<RivePropertyContent> Properties)
{
    public override string ToString() => $"{Name} ({Properties.Count} properties)";
}

/// <summary>A property of a view model: its name and kind, and for a view model property the id of the view model it refers to.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RivePropertyContent(string Name, RivePropertyKind Kind, int ViewModelReferenceId = default)
{
    public override string ToString() => $"{Name}: {Kind}";
}

/// <summary>The kind of a view model property.</summary>
[Smell(SymbolSmell.Advanced)]
public enum RivePropertyKind
{
    None = 0,
    String = 1,
    Number = 2,
    Boolean = 3,
    Color = 4,
    List = 5,
    EnumType = 6,
    Trigger = 7,
    ViewModel = 8,
    Integer = 9,
    SymbolListIndex = 10,
    AssetImage = 11,
    Artboard = 12,
}
