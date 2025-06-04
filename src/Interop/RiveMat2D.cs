namespace VL.Rive.Interop;

using System.Numerics;
using static Methods;

partial struct RiveMat2D
{
    public unsafe RiveMat2D InvertOrIdentity()
    {
        var self = this;
        return rive_Mat2D_InvertOrIdentity(&self);
    }

    public static unsafe Vector2 operator *(RiveMat2D mat, Vector2 v)
    {
        float* m = &mat.values.e0;
        return new Vector2(
            m[0]* v.X + m[2]* v.Y + m[4],
            m[1]* v.X + m[3]* v.Y + m[5]
        );
    }
}
