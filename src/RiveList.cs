using System.Collections;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;
using static VL.Rive.Interop.Methods;

namespace VL.Rive;

/// <summary>
/// A list property of a view model instance. The items are view model instances; one added here stays alive for as
/// long as it is in the list, even if the object that was added is disposed or collected. The same item always comes
/// back as the same object.
/// </summary>
[Smell(SymbolSmell.Advanced)]
public sealed class RiveList : IReadOnlyList<RiveViewModel>, IList<RiveViewModel>, IRiveValue<RiveList>
{
    private readonly RiveContext riveContext;
    // The native list belongs to the instance that has it as a property - keep that alive
    private readonly RiveViewModel owner;
    private readonly nint handle;

    internal RiveList(RiveContext riveContext, RiveViewModel owner, nint handle)
    {
        this.riveContext = riveContext;
        this.owner = owner;
        this.handle = handle;
    }

    public int Count
    {
        get
        {
            owner.ThrowIfDisposed();
            return (int)rive_ViewModelInstanceListRuntime_Size(handle);
        }
    }

    public bool IsReadOnly => false;

    public RiveViewModel this[int index]
    {
        get
        {
            owner.ThrowIfDisposed();
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Hands us one reference, which the view model object takes over (or releases if there is one already)
            var instance = rive_ViewModelInstanceListRuntime_At(handle, index);
            if (instance == default)
                throw new InvalidOperationException($"The list has no instance at index {index}.");
            return riveContext.WrapViewModel(instance);
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Insert first so that a failure leaves the list as it was
            Insert(index, value);
            RemoveAt(index + 1);
        }
    }

    RiveList? IRiveValue<RiveList>.TypedValue
    {
        get => this;
        set => ReplaceContentWith(value);
    }

    object? IRiveValue.Value
    {
        get => this;
        set => ReplaceContentWith(value);
    }

    // Assigning a list to a list property makes this list show the same items. The items are shared, not copied.
    private void ReplaceContentWith(object? value)
    {
        if (value is not RiveList other)
            throw new ArgumentException($"A list can only be assigned a {nameof(RiveList)}.", nameof(value));

        owner.ThrowIfDisposed();
        if (ReferenceEquals(other, this) || other.handle == handle)
            return;

        // Take the items first: this list might be the other one's source
        var items = other.ToArray();
        Clear();
        foreach (var item in items)
            Add(item);
    }

    public void Add(RiveViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        owner.ThrowIfDisposed();

        // The list takes its own reference to the instance
        rive_ViewModelInstanceListRuntime_AddInstance(handle, item.Handle);
    }

    public void Insert(int index, RiveViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        owner.ThrowIfDisposed();

        if (rive_ViewModelInstanceListRuntime_AddInstanceAt(handle, item.Handle, index) == 0)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>Removes the item, every time it is in the list.</summary>
    public void Remove(RiveViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        owner.ThrowIfDisposed();

        rive_ViewModelInstanceListRuntime_RemoveInstance(handle, item.Handle);
    }

    public void RemoveAt(int index)
    {
        owner.ThrowIfDisposed();
        if ((uint)index >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        rive_ViewModelInstanceListRuntime_RemoveInstanceAt(handle, index);
    }

    public void Clear()
    {
        for (int i = Count - 1; i >= 0; i--)
            rive_ViewModelInstanceListRuntime_RemoveInstanceAt(handle, i);
    }

    public int IndexOf(RiveViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        owner.ThrowIfDisposed();

        var target = item.InstanceHandle;
        var count = Count;
        for (int i = 0; i < count; i++)
        {
            var instance = rive_ViewModelInstanceListRuntime_At(handle, i);
            if (instance == default)
                continue;

            // Only looking: give the reference we were handed straight back
            var found = rive_ViewModelInstanceRuntime_Instance(instance) == target;
            rive_ViewModelInstanceRuntime_Release(instance);
            if (found)
                return i;
        }
        return -1;
    }

    public bool Contains(RiveViewModel item) => IndexOf(item) >= 0;

    public void CopyTo(RiveViewModel[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        var count = Count;
        if (array.Length - arrayIndex < count)
            throw new ArgumentException("The destination array is too small.", nameof(array));

        for (int i = 0; i < count; i++)
            array[arrayIndex + i] = this[i];
    }

    public IEnumerator<RiveViewModel> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool ICollection<RiveViewModel>.Remove(RiveViewModel item)
    {
        var count = Count;
        Remove(item);
        return Count < count;
    }
}
