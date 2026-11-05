using VL.Core;
using VL.Core.Import;

namespace VL.Rive;

/// <summary>Holds the artboard of a file: the same one comes out every frame until the file or the name changes.</summary>
internal sealed class ArtboardSlot
{
    private RiveFile? file;
    private string? name;
    private RiveArtboard? artboard;

    public RiveArtboard? Update(RiveFile? file, string? name)
    {
        name = string.IsNullOrEmpty(name) ? null : name;
        if (!ReferenceEquals(file, this.file) || name != this.name)
        {
            // Remember first: a name that does not exist is reported once, not every frame
            this.file = file;
            this.name = name;
            artboard = null;

            if (file != null && !file.IsDisposed)
            {
                artboard = file.GetArtboard(name)
                    ?? throw new ArgumentException($"Rive artboard '{name}' not found in file '{file.FilePath}'.", nameof(name));
            }
        }
        return artboard is { IsDisposed: false } ? artboard : null;
    }
}

/// <summary>An artboard of a Rive file, by name.</summary>
/// <remarks>
/// The artboard is what the file says about it, nothing that plays: give it to a Scene node to show it, and to
/// DefaultViewModel for an instance of its view model.
/// </remarks>
[ProcessNode(Name = "Artboard", Summary = "An artboard of a Rive file", Tags = "rive")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveArtboardNode
{
    private readonly ArtboardSlot slot = new();

    /// <param name="file">The file the artboard is taken from</param>
    /// <param name="name">The name of the artboard, the editor offers the ones in the file and shows it as the label of the node</param>
    /// <param name="artboard">The artboard, null while there is no file</param>
    public void Update(RiveFile? file, [NodeLabel, ChoiceProvider(typeof(RiveArtboardChoiceProvider))] string? name, out RiveArtboard? artboard)
        => artboard = slot.Update(file, name);
}

/// <summary>The default artboard of a Rive file.</summary>
/// <remarks>See <see cref="RiveArtboardNode"/>.</remarks>
[ProcessNode(Name = "DefaultArtboard", Summary = "The default artboard of a Rive file", Tags = "rive")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveDefaultArtboardNode
{
    private readonly ArtboardSlot slot = new();

    /// <param name="file">The file the artboard is taken from</param>
    /// <param name="artboard">The artboard, null while there is no file</param>
    public void Update(RiveFile? file, out RiveArtboard? artboard)
        => artboard = slot.Update(file, null);
}

/// <summary>A scene (a state machine or an animation) of a Rive artboard, by name, bound to a view model instance.</summary>
/// <remarks>
/// Each scene node makes an instance of the artboard of its own, so scenes of the same artboard have a state and a
/// view model of their own. The scene is advanced here, once per frame, and changes it makes to the view model
/// instance are told in the same frame. Renderers only draw it, so any number of them can show the same scene.
/// </remarks>
[ProcessNode(Name = "Scene", Summary = "A scene (state machine or animation) of a Rive artboard", Tags = "rive,state machine,animation,data binding")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveSceneNode : IDisposable
{
    private readonly RiveSceneSlot slot;

    public RiveSceneNode(NodeContext nodeContext) => slot = new RiveSceneSlot(nodeContext);

    /// <param name="artboard">The artboard the scene is taken from</param>
    /// <param name="name">The name of the state machine or animation, the editor offers the ones of the artboard and shows it as the label of the node</param>
    /// <param name="viewModel">The view model instance to bind. Another one replaces the first. With none, an instance of the view model of the artboard is bound: take it from DefaultViewModel to get at its values.</param>
    /// <param name="scene">The scene, null while there is no artboard</param>
    public void Update(
        RiveArtboard? artboard,
        [NodeLabel, ChoiceProvider(typeof(RiveSceneChoiceProvider))] string? name,
        RiveViewModel? viewModel,
        out RiveScene? scene)
        => scene = slot.Update(artboard, name, viewModel);

    public void Dispose() => slot.Dispose();
}

/// <summary>The default scene of a Rive artboard, bound to a view model instance.</summary>
/// <remarks>See <see cref="RiveSceneNode"/>.</remarks>
[ProcessNode(Name = "DefaultScene", Summary = "The default scene of a Rive artboard", Tags = "rive,state machine,animation,data binding")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveDefaultSceneNode : IDisposable
{
    private readonly RiveSceneSlot slot;

    public RiveDefaultSceneNode(NodeContext nodeContext) => slot = new RiveSceneSlot(nodeContext);

    /// <param name="artboard">The artboard the scene is taken from</param>
    /// <param name="viewModel">The view model instance to bind. Another one replaces the first. With none, an instance of the view model of the artboard is bound: take it from DefaultViewModel to get at its values.</param>
    /// <param name="scene">The scene, null while there is no artboard</param>
    public void Update(RiveArtboard? artboard, RiveViewModel? viewModel, out RiveScene? scene)
        => scene = slot.Update(artboard, null, viewModel);

    public void Dispose() => slot.Dispose();
}
