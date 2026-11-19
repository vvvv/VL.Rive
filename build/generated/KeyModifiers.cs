namespace VL.Rive.Interop
{
    [NativeTypeName("uint8_t")]
    internal enum KeyModifiers : byte
    {
        none = 0,
        shift = 1 << 0,
        ctrl = 1 << 1,
        alt = 1 << 2,
        meta = 1 << 3,
    }
}
