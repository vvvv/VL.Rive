using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>What a file's context knows about its view models, and what happens to them when the file goes.</summary>
[TestFixture]
public class ContextTests
{
    private RiveFactory factory = null!;
    private RiveContext context = null!;

    [SetUp]
    public void SetUp()
    {
        factory = new RiveFactory();
        var file = factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv"));
        context = new RiveContext(nodeContext: null, file);
    }

    [TearDown]
    public void TearDown()
    {
        context.Dispose();
        factory.Dispose();
    }

    [Test]
    public void TheContextTellsAboutTheChangesOfEveryInstanceOfTheFile()
    {
        var type = context.ViewModels.First(t => t.Properties.Any(p => p.RiveProperty.Type == RiveDataType.Number));
        var property = type.Properties.First(p => p.RiveProperty.Type == RiveDataType.Number).Name;
        var first = type.CreateInstance();
        var second = type.CreateInstance();
        var changed = new List<string>();
        second.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);
        second.AcknowledgeChanges();
        changed.Clear();

        second.SetObject(property, 42f);

        // Another instance does not tell about it, the context tells about all of them
        first.AcknowledgeChanges();
        Assert.That(changed, Is.Empty);
        context.AcknowledgeChanges();
        Assert.That(changed, Is.EqualTo(new[] { property }));

        // And only once
        context.AcknowledgeChanges();
        Assert.That(changed, Has.Count.EqualTo(1));
    }

    // The values of the properties a patch can read without the editor knowing more about them
    private static Dictionary<string, object?> SimpleValues(RiveViewModelType type, RiveViewModel instance)
        => type.Properties
            .Where(p => p.RiveProperty.Type is RiveDataType.Number or RiveDataType.Boolean or RiveDataType.String)
            .ToDictionary(p => p.Name, p => instance.GetObject(p.Name));

    [Test]
    public void AnInstanceDefinedInTheFileCanBeCopiedByItsName()
    {
        var type = context.ViewModels.First(t => t.InstanceNames.Length > 0);

        var copy = type.CreateInstanceFromName(type.InstanceNames[0]);

        Assert.That(copy, Is.Not.Null);
        Assert.That(copy!.ViewModelId, Is.EqualTo(context.ViewModels.IndexOf(type)));
        Assert.That(copy, Is.Not.SameAs(type.CreateInstanceFromName(type.InstanceNames[0])), "every call makes a new instance");

        // The first instance of a view model is its default one
        Assert.That(SimpleValues(type, copy), Is.EqualTo(SimpleValues(type, type.CreateDefaultInstance())));

        Assert.That(type.CreateInstanceFromName("There is no such instance"), Is.Null);
    }

    [Test]
    public void InstancesWithoutANameOrWithTheSameOneGetANameOfTheirOwn()
    {
        var names = RiveViewModelType.UniqueInstanceNames(["", "A", "A", "Instance", "  ", null, "A"]);

        // The names there are stay as they are, the made up ones do not take one of them
        Assert.That(names, Is.EqualTo(new[] { "Instance 2", "A", "A 2", "Instance", "Instance 3", "Instance 4", "A 3" }));
        Assert.That(names, Is.Unique);
        Assert.That(RiveViewModelType.UniqueInstanceNames([]), Is.Empty);
    }

    [Test]
    public void EveryNameOfAnInstanceChoosesItsOwnInstance()
    {
        // The two instances of Coin_Gem_Value differ in their value
        var type = context.FindViewModel("Coin_Gem_Value")!;
        Assert.That(type.InstanceNames, Is.EquivalentTo(new[] { "Gem-instance", "Coin-instance" }));

        var values = type.InstanceNames.ToDictionary(name => name, name => type.CreateInstanceFromName(name)!.GetObject("Item_Value"));

        Assert.That(values["Gem-instance"], Is.Not.EqualTo(values["Coin-instance"]));
    }

    [Test]
    public void ANamedInstanceBringsTheValuesTheFileDefinesForIt()
    {
        // A blank instance has the values of the types, an instance of the file the ones that were set in the editor
        var differs = context.ViewModels.Any(type => type.InstanceNames.Length > 0
            && !SimpleValues(type, type.CreateInstanceFromName(type.InstanceNames[0])!).SequenceEqual(SimpleValues(type, type.CreateInstance())));

        Assert.That(differs, Is.True);
    }

    [Test]
    public void ViewModelsAreFoundByName()
    {
        foreach (var type in context.ViewModels)
            Assert.That(context.FindViewModel(type.Name), Is.SameAs(type));

        Assert.That(context.FindViewModel("There is no such view model"), Is.Null);
    }

    [Test]
    public void InstancesAreDisposedWithTheFileTheyBelongTo()
    {
        var type = context.ViewModels.First(t => t.Properties.Length > 0);
        var instance = type.CreateInstance();
        var property = type.Properties[0].Name;
        Assert.DoesNotThrow(() => instance.GetObject(property));

        context.Dispose();

        Assert.That(instance.IsDisposed, Is.True);
        Assert.Throws<ObjectDisposedException>(() => instance.GetObject(property));
    }

    [Test]
    public void AnInstanceThatIsBeingCreatedByNameIsHeldUntilItIsDisposed()
    {
        var type = context.ViewModels.First();
        var first = context.FindViewModel(type.Name)!.CreateInstance();
        var second = context.FindViewModel(type.Name)!.CreateInstance();

        Assert.That(second, Is.Not.SameAs(first), "every instance is a new one");

        first.Dispose();
        Assert.That(second.IsDisposed, Is.False, "the others are unaffected");
    }
}
