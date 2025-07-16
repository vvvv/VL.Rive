using System.Runtime.CompilerServices;

namespace VL.Rive.Interop
{
    internal partial struct RiveMat2D
    {
        [NativeTypeName("float[6]")]
        public _values_e__FixedBuffer values;

        [InlineArray(6)]
        public partial struct _values_e__FixedBuffer
        {
            public float e0;
        }
    }
}
