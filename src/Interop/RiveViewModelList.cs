using System.Collections;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveViewModelList : IReadOnlyList<RiveViewModelInstance>, IList<RiveViewModelInstance>
{
    private readonly nint handle;

    public RiveViewModelList(nint handle) 
    {
        this.handle = handle;
    }

    public RiveViewModelInstance this[int index]
    {
        get => new RiveViewModelInstance(rive_ViewModelInstanceListRuntime_At(handle, index));
        set => throw new NotSupportedException();
    }

    public int Count => (int)rive_ViewModelInstanceListRuntime_Size(handle);

    public bool IsReadOnly => false;

    public void Add(RiveViewModelInstance item)
    {
        rive_ViewModelInstanceListRuntime_AddInstance(handle, item.Handle);
    }

    public void Insert(int index, RiveViewModelInstance item)
    {
        rive_ViewModelInstanceListRuntime_AddInstanceAt(handle, item.Handle, index);
    }

    public void Remove(RiveViewModelInstance item)
    {
        rive_ViewModelInstanceListRuntime_RemoveInstance(handle, item.Handle);
    }

    public void RemoveAt(int index)
    {
        rive_ViewModelInstanceListRuntime_RemoveInstanceAt(handle, index);
    }

    public IEnumerator<RiveViewModelInstance> GetEnumerator()
    {
        var i = 0;
        while (i < Count)
            yield return new RiveViewModelInstance(rive_ViewModelInstanceListRuntime_At(handle, i++));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int IndexOf(RiveViewModelInstance item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        var count = Count;
        for (int i = count - 1; i >= 0; i--)
            rive_ViewModelInstanceListRuntime_RemoveInstanceAt(handle, i);
    }

    public bool Contains(RiveViewModelInstance item)
    {
        var count = Count;
        for (int i = 0; i < count; i++)
        {
            if (rive_ViewModelInstanceListRuntime_At(handle, i) == item.Handle)
                return true;
        }
        return false;
    }

    public void CopyTo(RiveViewModelInstance[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    bool ICollection<RiveViewModelInstance>.Remove(RiveViewModelInstance item)
    {
        var count = Count;
        Remove(item);
        return Count < count;
    }
}
