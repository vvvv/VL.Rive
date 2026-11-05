using Microsoft.Extensions.DependencyInjection;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lang;
using VL.Lib.Animation;

namespace VL.Rive;

/// <summary>
/// What a scene node holds: an instance of an artboard of its own, one of its scenes, and the view model instance
/// bound to it. All three stay the same every frame until the artboard or the name changes. Advances the scene by the
/// time of the frame.
/// </summary>
internal sealed class RiveSceneSlot : IDisposable
{
    private readonly NodeContext nodeContext;
    private readonly IFrameClock frameClock;
    private readonly SerialDisposable warning = new();

    private RiveArtboard? artboard;
    private string? sceneName;
    private RiveArtboardInstance? instance;
    private RiveScene? scene;
    private RiveViewModel? defaultViewModel;
    private RiveViewModel? boundViewModel;

    public RiveSceneSlot(NodeContext nodeContext)
    {
        this.nodeContext = nodeContext;
        frameClock = nodeContext.AppHost.Services.GetRequiredService<IFrameClock>();
    }

    public RiveArtboardInstance? Instance => instance is { IsDisposed: false } ? instance : null;

    /// <summary>The view model instance that is bound: the one that came in, or the default one of the artboard.</summary>
    public RiveViewModel? BoundViewModel => boundViewModel is { IsDisposed: false } ? boundViewModel : null;

    /// <param name="viewModel">The instance to bind. The default one of the artboard if there is none.</param>
    /// <returns>The scene, null while there is no artboard.</returns>
    public RiveScene? Update(RiveArtboard? artboard, string? sceneName, RiveViewModel? viewModel)
    {
        sceneName = string.IsNullOrEmpty(sceneName) ? null : sceneName;

        var newInstance = !ReferenceEquals(artboard, this.artboard);
        if (newInstance)
        {
            Release();

            // Remember first: a name that does not exist is reported once, not every frame
            this.artboard = artboard;
            this.sceneName = null;

            if (artboard != null && !artboard.IsDisposed)
                instance = artboard.CreateInstance();
        }

        if (instance is null || instance.IsDisposed)
            return null;

        if (newInstance || sceneName != this.sceneName)
        {
            scene?.Dispose();
            scene = null;
            this.sceneName = sceneName;

            scene = instance.GetScene(sceneName)
                ?? throw new ArgumentException($"Rive scene '{sceneName}' not found in artboard '{instance.Name}'.", nameof(sceneName));
        }

        // A disposed instance counts as none, and none means the one the artboard is made for
        if (viewModel != null && viewModel.IsDisposed)
            viewModel = null;
        if (viewModel is null)
        {
            defaultViewModel ??= instance.FileHandle.Context.ViewModels.ElementAtOrDefault(instance.ViewModelId)?.CreateDefaultInstance();
            viewModel = defaultViewModel;
        }

        // Bind when the instance changes: another one replaces the first
        if (!ReferenceEquals(viewModel, boundViewModel))
        {
            // Shown on the node for as long as this instance is bound
            warning.Disposable = viewModel != null && viewModel.ViewModelId != instance.ViewModelId
                ? nodeContext.AddPersistentMessage(MessageSeverity.Warning,
                    $"The view model instance is not an instance of the view model of the artboard '{instance.Name}', so the artboard will not show its values.")
                : null;

            instance.BindViewModel(viewModel);
            boundViewModel = viewModel;
        }

        scene!.Scene.AdvanceAndApply((float)frameClock.TimeDifference);

        // Tells whoever listens to the bound instance what the scene changed in this frame, whichever node made it
        viewModel?.AcknowledgeChanges();

        return scene;
    }

    private void Release()
    {
        warning.Disposable = null;
        boundViewModel = null;
        scene = null;
        Interlocked.Exchange(ref defaultViewModel, null)?.Dispose();
        Interlocked.Exchange(ref instance, null)?.Dispose(); // with its scenes
    }

    public void Dispose()
    {
        warning.Dispose();
        Release();
    }
}
