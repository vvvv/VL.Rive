using System.Runtime.CompilerServices;
using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>
/// Lists and the lifetime of view model instances: who owns the native instance of an item, an instance a user
/// created and added to a list, a nested instance.
/// </summary>
[TestFixture]
public class ListTests
{
    private RiveFactory factory = null!;
    private RiveContext context = null!;

    [SetUp]
    public void SetUp()
    {
        factory = new RiveFactory();
        var file = factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-lists.riv"));
        context = new RiveContext(nodeContext: null, file);
    }

    [TearDown]
    public void TearDown()
    {
        context.Dispose();
        factory.Dispose();
    }

    // The first list property of the file, with the type its items have
    (RiveViewModelType Type, RiveViewModelProperty Property, RiveViewModelType ItemType) ListProperty()
    {
        foreach (var type in context.ViewModels)
        {
            foreach (var property in type.Properties.Where(p => p.RiveProperty.Type == RiveDataType.List))
                return (type, property, context.ViewModels[property.RiveProperty.ViewModelReferenceId]);
        }
        throw new AssertionException("The file has no list property.");
    }

    (RiveViewModel Owner, RiveList List, RiveViewModelType ItemType) NewList()
    {
        var (type, property, itemType) = ListProperty();
        var owner = type.CreateInstance();
        return (owner, (RiveList)owner.GetObject(property.Name)!, itemType);
    }

    [Test]
    public void AnAddedInstanceIsFoundInTheListAndComesBackAsTheSameObject()
    {
        var (_, list, itemType) = NewList();
        var item = itemType.CreateInstance();

        list.Add(item);

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list.Contains(item), Is.True);
        Assert.That(list.IndexOf(item), Is.EqualTo(0));
        Assert.That(list[0], Is.SameAs(item));
        Assert.That(list.Single(), Is.SameAs(item));
    }

    [Test]
    public void ARemovedInstanceStaysUsable()
    {
        var (_, list, itemType) = NewList();
        var item = itemType.CreateInstance();
        list.Add(item);

        list.Remove(item);

        Assert.That(list.Count, Is.EqualTo(0));
        Assert.That(list.Contains(item), Is.False);
        Assert.That(list.IndexOf(item), Is.EqualTo(-1));
        Assert.DoesNotThrow(() => ReadAll(item));
    }

    [Test]
    public void AnInstanceInAListOutlivesTheObjectThatAddedIt()
    {
        var (_, list, itemType) = NewList();
        AddAndDrop(list, itemType);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.DoesNotThrow(() => ReadAll(list[0]));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AddAndDrop(RiveList list, RiveViewModelType itemType)
        {
            var item = itemType.CreateInstance();
            list.Add(item);
            item.Dispose();
        }
    }

    [Test]
    public void InsertAndTheIndexerSetterPlaceItems()
    {
        var (_, list, itemType) = NewList();
        var a = itemType.CreateInstance();
        var b = itemType.CreateInstance();
        var c = itemType.CreateInstance();

        list.Add(a);
        list.Insert(0, b);
        Assert.That(list.ToArray(), Is.EqualTo(new[] { b, a }));

        list[0] = c;
        Assert.That(list.ToArray(), Is.EqualTo(new[] { c, a }));
        Assert.That(list.IndexOf(b), Is.EqualTo(-1));

        var copy = new RiveViewModel[3];
        list.CopyTo(copy, 1);
        Assert.That(copy, Is.EqualTo(new[] { null, c, a }));
    }

    [Test]
    public void AssigningAListCopiesItsContents()
    {
        var (_, source, itemType) = NewList();
        var a = itemType.CreateInstance();
        var b = itemType.CreateInstance();
        source.Add(a);
        source.Add(b);

        var (type, property, _) = ListProperty();
        var other = type.CreateInstance();
        other.SetObject(property.Name, source);

        var target = (RiveList)other.GetObject(property.Name)!;
        Assert.That(target.ToArray(), Is.EqualTo(new[] { a, b }), "same items");

        source.Remove(a);
        Assert.That(target.Count, Is.EqualTo(2), "the lists are separate");
    }

    [Test]
    public void ADisposedInstanceRefusesToWork()
    {
        var (owner, list, itemType) = NewList();
        var item = itemType.CreateInstance();

        item.Dispose();
        item.Dispose(); // harmless

        Assert.Throws<ObjectDisposedException>(() => ReadAll(item));
        Assert.Throws<ObjectDisposedException>(() => list.Add(item));

        owner.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = list.Count);
    }

    [Test]
    public void ANestedInstanceCanBeReplaced()
    {
        var nested = context.ViewModels
            .SelectMany(type => type.Properties.Where(p => p.RiveProperty.Type == RiveDataType.ViewModel).Select(p => (type, p)))
            .FirstOrDefault();
        if (nested.p is null)
            Assert.Inconclusive("The file has no nested view model property.");

        var parent = nested.type.CreateInstance();
        var replacement = context.ViewModels[nested.p.RiveProperty.ViewModelReferenceId].CreateInstance();

        parent.SetObject(nested.p.Name, replacement);

        Assert.That(parent.GetObject(nested.p.Name), Is.SameAs(replacement));
    }

    [Test]
    public void InstancesCanBeFinalizedAfterTheFileIsGone()
    {
        CreateGarbage();

        context.Dispose();
        factory.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.Pass("The finalizers ran without taking the process down.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        void CreateGarbage()
        {
            for (var i = 0; i < 10; i++)
            {
                var (_, list, itemType) = NewList();
                list.Add(itemType.CreateInstance());
                _ = list[0];
            }
        }
    }

    private static void ReadAll(RiveViewModel viewModel)
    {
        foreach (var property in viewModel.Type.Properties)
            viewModel.GetObject(property.Name);
    }
}
