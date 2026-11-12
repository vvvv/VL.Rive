using System.Reactive.Disposables;
using VL.Core;
using VL.Core.Import;
using VL.Lang;
using VL.Rive.Internal;

namespace VL.Rive;

/// <summary>
/// Creates an instance of one of the view models of a Rive file, and holds on to it: the same instance comes out every frame until the name or the file changes.
/// </summary>
/// <remarks>
/// Instances belong to the file they were created from. The instance is released when the node goes away. If it was added to a list or assigned to a nested property by then, the list or the property keeps it alive.
/// </remarks>
[ProcessNode(Name = "ViewModel", Summary = "Creates an instance of a view model of a Rive file", Tags = "rive,data binding,instance")]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveViewModelNode : IDisposable
{
    private readonly NodeContext nodeContext;
    private readonly SerialDisposable warning = new();
    private RiveContext? file;
    private string? name;
    private string? instanceName;
    private RiveViewModel? viewModel;

    public RiveViewModelNode(NodeContext nodeContext) => this.nodeContext = nodeContext;

    /// <param name="input">The file the view model is taken from</param>
    /// <param name="name">The name of the view model, the editor offers the ones in the file and shows it as the label of the node</param>
    /// <param name="instanceName">The instance of the view model that is defined in the file to make a copy of, the editor offers the ones there are (the first is the default one). A blank instance if empty.</param>
    /// <param name="viewModel">The instance, null while there is no file, no name is given or the file has no view model or instance of that name (the node shows a warning then)</param>
    public void Update(
        RiveContext? input,
        [NodeLabel, ChoiceProvider(typeof(RiveViewModelChoiceProvider))] string? name,
        [ChoiceProvider(typeof(RiveViewModelInstanceChoiceProvider))] string? instanceName,
        out RiveViewModel? viewModel)
    {
        instanceName = string.IsNullOrEmpty(instanceName) ? null : instanceName;
        if (!ReferenceEquals(input, this.file) || name != this.name || instanceName != this.instanceName)
        {
            Release();

            this.file = input;
            this.name = name;
            this.instanceName = instanceName;

            var context = input is { IsDisposed: false } ? input : null;
            if (context != null && !string.IsNullOrEmpty(name))
            {
                var type = context.FindViewModel(name);
                if (type is null)
                {
                    // Shown on the node until the file or the name changes
                    warning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                        $"The Rive file '{input!.FilePath}' has no view model named '{name}'.");
                }
                else if (instanceName is null)
                {
                    this.viewModel = type.CreateInstance();
                }
                else
                {
                    this.viewModel = type.CreateInstanceFromName(instanceName);
                    if (this.viewModel is null)
                    {
                        warning.Disposable = nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                            $"The view model '{name}' of the Rive file '{input!.FilePath}' has no instance named '{instanceName}'.");
                    }
                }
            }
        }

        // Tells whoever listens to the instance what has changed since the last frame
        this.viewModel?.AcknowledgeChanges();
        viewModel = this.viewModel;
    }

    private void Release()
    {
        warning.Disposable = null;
        Interlocked.Exchange(ref viewModel, null)?.Dispose();
    }

    public void Dispose()
    {
        warning.Dispose();
        Release();
    }
}