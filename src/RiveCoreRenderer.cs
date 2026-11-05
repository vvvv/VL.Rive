using Stride.Core.Mathematics;
using Stride.Rendering;
using System.ComponentModel;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Collections;

namespace VL.Rive;

/// <summary>
/// Draws a scene of a Rive file and passes pointer input on to it. Gets what it draws from the nodes
/// RiveFile, Artboard and Scene (or their Default variants).
/// </summary>
/// <remarks>
/// The scene node binds the view model and advances the scene, the renderer only draws. So renderers can share a
/// scene, they show the same state, and renderers of the same file draw one after the other with the render
/// context of the file.
/// </remarks>
[ProcessNode(Name = "RiveRenderer (Core)", HasStateOutput = true, FragmentSelection = FragmentSelection.Explicit)]
[Smell(SymbolSmell.Advanced)]
public sealed class RiveCoreRenderer : RendererBase
{
    private readonly RiveSceneDrawer drawer;

    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public RiveCoreRenderer([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
    {
        drawer = new RiveSceneDrawer(nodeContext.GetLogger());
    }

    /// <param name="scene">The scene to draw</param>
    [Fragment]
    [Smell(SymbolSmell.Internal)]
    public void Update(
        RiveScene? scene,
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveFit fit,
        [Pin(Visibility = Model.PinVisibility.Optional)] RiveAlignment alignment,
        [Pin(Visibility = Model.PinVisibility.Optional)] Optional<RectangleF> frame,
        [Pin(Visibility = Model.PinVisibility.Optional)] Optional<RectangleF> content,
        [Pin(Visibility = Model.PinVisibility.Optional)] [DefaultValue(1f)] float scaleFactor)
    {
        drawer.Fit = fit;
        drawer.Alignment = alignment;
        drawer.Frame = frame;
        drawer.Content = content;
        drawer.ScaleFactor = scaleFactor;
        drawer.Scene = scene != null && !scene.IsDisposed ? scene : null;
    }

    protected override void DrawCore(RenderDrawContext context) => drawer.Draw(context);

    protected override void Destroy()
    {
        drawer.Dispose();
        base.Destroy();
    }
}
