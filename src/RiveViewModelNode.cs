using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;

namespace VL.Rive;

/// <summary>
/// Creates an instance of one of the view models in the Rive file a <see cref="RiveRenderer"/> has loaded, and holds
/// on to it: the same instance comes out every frame until the name, the renderer or the renderer's file changes.
/// </summary>
/// <remarks>
/// Instances belong to the file they were created from, which is why the renderer is the context: it loads the file
/// and tells when it changes. The instance is released when the node goes away. If it was added to a list or assigned
/// to a nested property by then, the list or the property keeps it alive.
/// </remarks>
[ProcessNode(Name = "ViewModel", Summary = "Creates an instance of a view model of the file a Rive renderer has loaded", Tags = "rive,data binding,instance")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveViewModelNode : IDisposable
{
    private RiveRenderer? renderer;
    private int fileVersion;
    private string? name;
    private RiveViewModel? viewModel;

    /// <param name="renderer">The renderer that has loaded the file the view model is taken from</param>
    /// <param name="name">The name of the view model, the editor offers the ones in the file</param>
    /// <param name="viewModel">The instance, null while the renderer has no file or no name is given</param>
    public void Update(RiveRenderer? renderer, [ChoiceProvider(typeof(RiveViewModelChoiceProvider))] string? name, out RiveViewModel? viewModel)
    {
        var version = renderer?.FileVersion ?? 0;
        if (!ReferenceEquals(renderer, this.renderer) || version != fileVersion || name != this.name)
        {
            Release();

            // Remember first: a name that does not exist is reported once, not every frame
            this.renderer = renderer;
            this.fileVersion = version;
            this.name = name;

            var context = renderer?.RiveContext;
            if (context != null && !string.IsNullOrEmpty(name))
            {
                var type = context.FindViewModel(name)
                    ?? throw new ArgumentException($"Rive view model '{name}' not found in the file of the renderer.", nameof(name));
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
