namespace RiveSharpInterop
{
    internal unsafe partial struct RivePropertyData
    {
        public int type;

        [NativeTypeName("const char *")]
        public sbyte* name;
    }
}
