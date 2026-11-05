using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>
/// An instance of an <see cref="RiveArtboard"/>, with a state and a view model binding of its own. Gives scenes (state machines, animations).
/// </summary>
internal sealed class RiveArtboardInstance : IDisposable
{
    private readonly List<IDisposable> scenes = new();
    private readonly NativeRiveArtboardInstance instance;

    internal RiveArtboardInstance(RiveArtboard artboard, NativeRiveArtboardInstance instance)
    {
        Artboard = artboard;
        this.instance = instance;
    }

    public RiveArtboard Artboard { get; }

    public RiveFile FileHandle => Artboard.File;

    public bool IsDisposed { get; private set; }

    internal NativeRiveArtboardInstance Instance
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return instance;
        }
    }

    internal RiveArtboardInfo? GetInfo() => IsDisposed ? null : Artboard.GetInfo();

    public string Name => Artboard.Name;

    internal int ViewModelId => Artboard.ViewModelId;

    internal RiveAABB Bounds => Instance.Bounds;

    /// <summary>The scene with the given name (a state machine or an animation), the default one for no name. Null if there is none.</summary>
    internal RiveScene? GetScene(string? name)
    {
        var scene = string.IsNullOrEmpty(name) ? Instance.GetDefaultScene() : Instance.GetScene(name);
        if (scene is null)
            return null;

        var handle = new RiveScene(this, scene);
        lock (scenes)
            scenes.Add(handle);

        // The state machine binds the instance of the artboard too
        if (viewModel != null)
            scene.BindViewModelInstance(viewModel);
        return handle;
    }

    private RiveViewModel? viewModel;

    /// <summary>
    /// Binds the instance to the artboard and to the state machines made from it, replacing the one they have. Null unbinds.
    /// The data context lives on the artboard instance, so everything made from it shows the same instance.
    /// </summary>
    internal void BindViewModel(RiveViewModel? viewModel)
    {
        // For an animation only the artboard can bind, a state machine binds itself too
        Instance.BindViewModelInstance(viewModel);
        this.viewModel = viewModel;
        RiveScene[] current;
        lock (scenes)
            current = scenes.OfType<RiveScene>().ToArray();
        foreach (var scene in current)
            if (!scene.IsDisposed)
                scene.Scene.BindViewModelInstance(viewModel);
    }

    internal void Untrack(IDisposable scene)
    {
        lock (scenes)
            scenes.Remove(scene);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        // The scenes refer to the artboard
        List<IDisposable> toDispose;
        lock (scenes)
        {
            toDispose = new List<IDisposable>(scenes);
            scenes.Clear();
        }
        for (var i = toDispose.Count - 1; i >= 0; i--)
            toDispose[i].Dispose();

        instance.Dispose();
        FileHandle.Untrack(this);
    }
}
