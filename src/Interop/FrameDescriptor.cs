using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiveSharpInterop;

// Specifies what to do with the render target at the beginning of a flush.
enum LoadAction
{
    Clear,
    PreserveRenderTarget,
    DontCare,
};

// Options for controlling how and where a frame is rendered.
struct FrameDescriptor
{
    public uint RenderTargetWidth;
    public uint RenderTargetHeight;
    public LoadAction LoadAction;
    public uint ClearColor;
    public int MsaaSampleCount;
    public bool DisableRasterOrdering;
    public bool Wireframe;
    public bool FillsDisabled;
    public bool StrokesDisabled;
    public bool ClockwiseFillOverride;

    // Explicit constructor to initialize fields with default values.
    public FrameDescriptor()
    {
        RenderTargetWidth = 0;
        RenderTargetHeight = 0;
        LoadAction = LoadAction.Clear;
        ClearColor = 0;
        MsaaSampleCount = 0;
        DisableRasterOrdering = false;
        Wireframe = false;
        FillsDisabled = false;
        StrokesDisabled = false;
        ClockwiseFillOverride = false;
    }
};
