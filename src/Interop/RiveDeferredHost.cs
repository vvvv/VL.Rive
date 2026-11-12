using VL.Rive.Internal;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

/// <summary>
/// Records a file's drawing - including what its scripts do on the GPU - and replays it against the render context once per frame. Needed for files with GPU scripts: a script's render pass cannot nest inside a screen frame that is already open on the render context. Must outlive everything imported through it.
/// </summary>
internal sealed class RiveDeferredHost : RiveObject
{
    private RiveRenderer? screenRenderer;

    private RiveDeferredHost(nint handle) : base(handle)
    {
    }

    /// <summary>Null when the native runtime has no GPU scripting support.</summary>
    public static RiveDeferredHost? TryCreate(RiveRenderContext renderContext)
    {
        var handle = rive_Deferred_Create(renderContext.DangerousGetHandle());
        return handle == default ? null : new RiveDeferredHost(handle);
    }

    public RiveFile LoadFile(string path) => Utils.LoadFile(path, rive_Deferred_Factory(handle));

    /// <summary>The renderer to draw the scene into. Owned by the host, valid until <see cref="EndFrame"/>.</summary>
    public RiveRenderer BeginFrame(int width, int height)
    {
        var renderer = rive_Deferred_BeginFrame(handle, (uint)width, (uint)height);
        if (renderer == default)
            throw new InvalidOperationException("Failed to begin a deferred Rive frame.");

        if (screenRenderer is null || screenRenderer.DangerousGetHandle() != renderer)
            screenRenderer = new RiveRenderer(renderer, ownsHandle: false);
        return screenRenderer;
    }

    public void EndFrame(RiveRenderTarget renderTarget, ulong frameNumber, ulong safeFrameNumber)
    {
        rive_Deferred_EndFrame(handle, renderTarget.DangerousGetHandle(), frameNumber, safeFrameNumber);
    }

    protected override bool ReleaseHandle()
    {
        screenRenderer = null;
        rive_Deferred_Destroy(handle);
        return true;
    }
}
