using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>A scene (a state machine or an animation) of a <see cref="RiveArtboardInstance"/>. This is what a renderer draws.</summary>
/// <remarks>
/// The scene is advanced by the node that made it, so any number of renderers can draw it.
/// </remarks>
public sealed class RiveScene : IDisposable
{
    private readonly NativeRiveScene scene;

    internal RiveScene(RiveArtboardInstance artboard, NativeRiveScene scene)
    {
        Artboard = artboard;
        this.scene = scene;
    }

    internal RiveArtboardInstance Artboard { get; }

    internal bool IsDisposed { get; private set; }

    internal NativeRiveScene Scene
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return scene;
        }
    }

    public override string ToString() => $"{scene.Name} ({Artboard.Name}, {Artboard.FileHandle})";

    void IDisposable.Dispose() => Dispose();

    internal void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        scene.Dispose();
        Artboard.Untrack(this);
    }
}
