using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;

namespace VL.Rive;

/// <summary>
/// Creates an instance of one of the view models of a Rive file, and holds on to it: the same instance comes out
/// every frame until the name or the file changes.
/// </summary>
/// <remarks>
/// Instances belong to the file they were created from. The instance is released when the node goes away. If it was
/// added to a list or assigned to a nested property by then, the list or the property keeps it alive.
/// </remarks>
[ProcessNode(Name = "ViewModel", Summary = "Creates an instance of a view model of a Rive file", Tags = "rive,data binding,instance")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveViewModelNode : IDisposable
{
    private RiveFile? file;
    private string? name;
    private RiveViewModel? viewModel;

    /// <param name="file">The file the view model is taken from</param>
    /// <param name="name">The name of the view model, the editor offers the ones in the file and shows it as the label of the node</param>
    /// <param name="viewModel">The instance, null while there is no file or no name is given</param>
    public void Update(RiveFile? file, [NodeLabel, ChoiceProvider(typeof(RiveViewModelChoiceProvider))] string? name, out RiveViewModel? viewModel)
    {
        if (!ReferenceEquals(file, this.file) || name != this.name)
        {
            Release();

            // Remember first: a name that does not exist is reported once, not every frame
            this.file = file;
            this.name = name;

            var context = file?.GetContextOrNull();
            if (context != null && !string.IsNullOrEmpty(name))
            {
                var type = context.FindViewModel(name)
                    ?? throw new ArgumentException($"Rive view model '{name}' not found in file '{file!.FilePath}'.", nameof(name));
                this.viewModel = type.CreateInstance();
            }
        }

        // Tells whoever listens to the instance what has changed since the last frame
        this.viewModel?.AcknowledgeChanges();
        viewModel = this.viewModel;
    }

    private void Release() => Interlocked.Exchange(ref viewModel, null)?.Dispose();

    public void Dispose() => Release();
}

/// <summary>
/// Creates an instance of the view model an artboard is made for, and holds on to it: the same instance comes out
/// every frame until the artboard changes. Give it to a Scene node to bind it there.
/// </summary>
/// <remarks>
/// This is the instance to change and to listen to. A Scene node with no view model binds an instance of its own,
/// which is not this one.
/// </remarks>
[ProcessNode(Name = "DefaultViewModel", Summary = "Creates an instance of the view model of a Rive artboard", Tags = "rive,data binding,instance")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveDefaultViewModelNode : IDisposable
{
    private RiveArtboard? artboard;
    private RiveViewModel? viewModel;

    /// <param name="artboard">The artboard the view model belongs to</param>
    /// <param name="viewModel">The instance, null while there is no artboard or it has no view model</param>
    public void Update(RiveArtboard? artboard, out RiveViewModel? viewModel)
    {
        if (!ReferenceEquals(artboard, this.artboard))
        {
            Release();
            this.artboard = artboard;

            var context = artboard?.File.GetContextOrNull();
            if (context != null)
                this.viewModel = context.ViewModels.ElementAtOrDefault(artboard!.ViewModelId)?.CreateDefaultInstance();
        }

        // Tells whoever listens to the instance what has changed since the last frame
        this.viewModel?.AcknowledgeChanges();
        viewModel = this.viewModel;
    }

    private void Release() => Interlocked.Exchange(ref viewModel, null)?.Dispose();

    public void Dispose() => Release();
}
