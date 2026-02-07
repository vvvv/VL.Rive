using System.Collections;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

[Smell(SymbolSmell.Advanced)]
public sealed class RiveList : IReadOnlyList<RiveViewModel>, IList<RiveViewModel>, IRiveValue<RiveList>
{
    private readonly nint handle;
    private readonly RiveContext riveContext;

    internal RiveList(RiveContext riveContext, nint handle)
    {
        this.riveContext = riveContext;
        this.handle = handle;
    }

    public RiveViewModel this[int index]
    {
        get => new RiveViewModel(riveContext, rive_ViewModelInstanceListRuntime_At(handle, index));
        set => throw new NotSupportedException();
    }

    public int Count => (int)rive_ViewModelInstanceListRuntime_Size(handle);

    public bool IsReadOnly => false;

    RiveList? IRiveValue<RiveList>.TypedValue 
    { 
        get => this; 
        set => throw new NotImplementedException(); 
    }
    object? IRiveValue.Value 
    { 
        get => this; 
        set => throw new NotImplementedException(); 
    }

    public void Add(RiveViewModel item)
    {
        rive_ViewModelInstanceListRuntime_AddInstance(handle, item.Handle);
    }

    public void Insert(int index, RiveViewModel item)
    {
        rive_ViewModelInstanceListRuntime_AddInstanceAt(handle, item.Handle, index);
    }

    public void Remove(RiveViewModel item)
    {
        rive_ViewModelInstanceListRuntime_RemoveInstance(handle, item.Handle);
    }

    public void RemoveAt(int index)
    {
        rive_ViewModelInstanceListRuntime_RemoveInstanceAt(handle, index);
    }

    public IEnumerator<RiveViewModel> GetEnumerator()
    {
        var i = 0;
        while (i < Count)
            yield return new RiveViewModel(riveContext, rive_ViewModelInstanceListRuntime_At(handle, i++));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int IndexOf(RiveViewModel item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        var count = Count;
        for (int i = count - 1; i >= 0; i--)
            rive_ViewModelInstanceListRuntime_RemoveInstanceAt(handle, i);
    }

    public bool Contains(RiveViewModel item)
    {
        var count = Count;
        for (int i = 0; i < count; i++)
        {
            if (rive_ViewModelInstanceListRuntime_At(handle, i) == item.Handle)
                return true;
        }
        return false;
    }

    public void CopyTo(RiveViewModel[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    bool ICollection<RiveViewModel>.Remove(RiveViewModel item)
    {
        var count = Count;
        Remove(item);
        return Count < count;
    }
}
