using VL.Lib.Collections;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>What a Rive file holds.</summary>
/// <param name="Artboards">The artboards, with their state machines and animations</param>
/// <param name="ViewModels">The view models, with their properties</param>
/// <param name="Enums">The enums the properties of the view models can have, with their values</param>
[Smell(SymbolSmell.Advanced)]
public sealed record RiveFileContent(Spread<RiveArtboardContent> Artboards, Spread<RiveViewModelContent> ViewModels, Spread<RiveEnumContent> Enums)
{
    internal static RiveFileContent From(RiveFile file) => new(
        file.Artboards.Select(a => new RiveArtboardContent(
            a.Name,
            a.StateMachines.Select(s => s.Name).ToSpread(),
            a.Animations.Select(x => x.Name).ToSpread(),
            a.DefaultViewModel)).ToSpread(),
        file.ViewModels.Select(v => new RiveViewModelContent(
            v.Name,
            v.Properties.Select(p => new RivePropertyContent(p.Name, (RivePropertyKind)(int)p.Type, p.ViewModelReferenceId, p.EnumId >= 0 && p.EnumId < file.Enums.Length ? file.Enums[p.EnumId].Name : null)).ToSpread())).ToSpread(),
        file.Enums.Select(e => new RiveEnumContent(e.Name, e.Values.ToSpread())).ToSpread());

    public override string ToString() => $"{Artboards.Count} artboards, {ViewModels.Count} view models, {Enums.Count} enums";
}

/// <summary>An artboard of a Rive file: the names of its state machines and animations, and the view model it is made for.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveArtboardContent(
    string Name,
    Spread<string> StateMachines,
    Spread<string> Animations,
    string? DefaultViewModel)
{
    public override string ToString()
        => $"{Name} ({StateMachines.Count} state machines, {Animations.Count} animations"
            + (DefaultViewModel is null ? ")" : $", view model {DefaultViewModel})");
}

/// <summary>A view model of a Rive file and its properties.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveViewModelContent(string Name, Spread<RivePropertyContent> Properties)
{
    public override string ToString() => $"{Name} ({Properties.Count} properties)";
}

/// <summary>A property of a view model: its name and kind, for a view model property the id of the view model it refers to, and for an enum property the name of its enum.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RivePropertyContent(string Name, RivePropertyKind Kind, int ViewModelReferenceId = default, string? EnumType = null)
{
    public override string ToString() => EnumType is null ? $"{Name}: {Kind}" : $"{Name}: {Kind} ({EnumType})";
}

/// <summary>An enum of a Rive file and its values.</summary>
[Smell(SymbolSmell.Advanced)]
public record struct RiveEnumContent(string Name, Spread<string> Values)
{
    public override string ToString() => $"{Name} ({string.Join(", ", Values)})";
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
