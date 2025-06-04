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
