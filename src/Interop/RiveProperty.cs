namespace VL.Rive.Interop;

internal abstract class RiveProperty
{
    protected readonly nint handle;

    protected RiveProperty(string name, Type type, nint handle)
    {
        Name = name;
        Type = type;
        this.handle = handle;
    }

    public string Name { get; }
    public Type Type { get; }
}
