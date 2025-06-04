namespace VL.Rive.Interop
{
    internal unsafe partial struct RivePropertyData
    {
        public int type;

        [NativeTypeName("const char *")]
        public sbyte* name;
    }
}
