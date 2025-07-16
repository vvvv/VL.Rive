using Stride.Core.Mathematics;
using VL.Core;

namespace VL.Rive.Interop;

partial struct RiveAABB
{
    public RiveAABB(float minX, float minY, float maxX, float maxY)
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
    }
}


static class RiveAABBExtensions
{
    public static RiveAABB ToNative(this RectangleF rectangle)
    {
        return new RiveAABB(rectangle.Left.HectoToRive(), rectangle.Top.HectoToRive(), rectangle.Right.HectoToRive(), rectangle.Bottom.HectoToRive());
    }

    public static RiveAABB ToNative(this Optional<RectangleF> rectangle, RiveAABB fallback)
    {
        if (rectangle.HasValue)
            return rectangle.Value.ToNative();
        return fallback;
    }

    public static float HectoToRive(this float value)
    {
        return value * 100.0f;
    }
}