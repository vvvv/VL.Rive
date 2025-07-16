namespace VL.Rive.Interop;

partial struct RiveAlignment
{
    public RiveAlignment(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public static readonly RiveAlignment topLeft = new RiveAlignment(-1.0f, -1.0f);
    public static readonly RiveAlignment topCenter = new RiveAlignment(0.0f, -1.0f);
    public static readonly RiveAlignment topRight = new RiveAlignment(1.0f, -1.0f);
    public static readonly RiveAlignment centerLeft = new RiveAlignment(-1.0f, 0.0f);
    public static readonly RiveAlignment center = new RiveAlignment(0.0f, 0.0f);
    public static readonly RiveAlignment centerRight = new RiveAlignment(1.0f, 0.0f);
    public static readonly RiveAlignment bottomLeft = new RiveAlignment(-1.0f, 1.0f);
    public static readonly RiveAlignment bottomCenter = new RiveAlignment(0.0f, 1.0f);
    public static readonly RiveAlignment bottomRight = new RiveAlignment(1.0f, 1.0f);
}
