namespace VL.Rive.Interop;

// Specifies what to do with the render target at the beginning of a flush.
enum LoadAction
{
    Clear,
    PreserveRenderTarget,
    DontCare,
};

// Various types of ordered dithering to reduce banding.
enum DitherMode
{
    None,
    InterleavedGradientNoise,
}

// Budget for interior triangulation of filled paths. Mirrors rive::gpu::TriangulationThresholds.
struct TriangulationThresholds
{
    public float MinArea;
    public nuint MaxVerbs;
    public float FrameBudgetMs;

    public TriangulationThresholds()
    {
        MinArea = 512f * 512f;
        MaxVerbs = 256;
        FrameBudgetMs = 2f;
    }
}

// Options for controlling how and where a frame is rendered.
// Must mirror RenderContext::FrameDescriptor (render_context.hpp) field by field.
struct FrameDescriptor
{
    public uint RenderTargetWidth;
    public uint RenderTargetHeight;
    public LoadAction LoadAction;
    public uint ClearColor;
    public uint MsaaSampleCount;
    public bool DisableRasterOrdering;
    public DitherMode DitherMode;
    public TriangulationThresholds TriangulationThresholds;
    public uint VirtualTileWidth;
    public uint VirtualTileHeight;
    public bool Wireframe;
    public bool FillsDisabled;
    public bool StrokesDisabled;
    public bool ClockwiseFillOverride;
    public bool InkbleedOverride;

    // Explicit constructor to initialize fields with default values.
    public FrameDescriptor()
    {
        RenderTargetWidth = 0;
        RenderTargetHeight = 0;
        LoadAction = LoadAction.Clear;
        ClearColor = 0;
        MsaaSampleCount = 0;
        DisableRasterOrdering = false;
        DitherMode = DitherMode.InterleavedGradientNoise;
        TriangulationThresholds = new TriangulationThresholds();
        VirtualTileWidth = 0;
        VirtualTileHeight = 0;
        Wireframe = false;
        FillsDisabled = false;
        StrokesDisabled = false;
        ClockwiseFillOverride = false;
        InkbleedOverride = false;
    }
};
