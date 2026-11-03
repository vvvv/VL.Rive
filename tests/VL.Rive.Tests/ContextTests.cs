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
