namespace VL.Rive.Interop;

internal abstract class RiveProperty : RiveObject
{
    protected RiveProperty(string name, Type type, nint handle, bool ownsHandle = true) : base(handle, ownsHandle)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }
    public Type Type { get; }
}
