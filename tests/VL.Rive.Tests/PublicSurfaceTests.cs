using System.Reflection;
using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>
/// What a patch sees of the types that flow between the nodes: vvvv imports every public member as a node, and shows
/// what ToString says in the tooltip.
/// </summary>
[TestFixture]
public class PublicSurfaceTests
{
    private static readonly Type[] PinTypes = [typeof(RiveContext), typeof(RiveViewModel), typeof(RiveList)];

    [TestCaseSource(nameof(PinTypes))]
    public void LifetimeIsNotPartOfThePublicSurface(Type type)
    {
        var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(m => m.Name);

        Assert.That(members, Has.None.EqualTo(nameof(IDisposable.Dispose)));
        Assert.That(members, Has.None.EqualTo("IsDisposed"));
    }

    [TestCaseSource(nameof(PinTypes))]
    public void TheTooltipSaysSomethingOfItsOwn(Type type)
    {
        Assert.That(type.GetMethod(nameof(ToString), Type.EmptyTypes)!.DeclaringType, Is.EqualTo(type));
    }

    [Test]
    public void TheWarningForAnotherViewModelNamesBothViewModels()
    {
        var message = RiveRenderer.MismatchMessage("Main", "Menu", "ItemList");

        Assert.That(message, Does.Contain("'Main'").And.Contain("'Menu'").And.Contain("'ItemList'"));
        Assert.That(RiveRenderer.MismatchMessage("Main", null, "ItemList"), Does.Contain("has no view model").And.Contain("'ItemList'"));
    }

    [Test]
    public void AViewModelInstanceShowsTheNameOfItsViewModelAlsoOnceItIsDisposed()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));
        var type = context.ViewModels.First();
        var instance = type.CreateInstance();

        Assert.That(instance.ToString(), Is.EqualTo(type.Name));

        instance.Dispose();

        Assert.That(instance.ToString(), Does.StartWith(type.Name));
    }

    [Test]
    public void AListShowsHowManyItemsItHas()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-lists.riv")));
        var (type, property) = context.ViewModels
            .SelectMany(t => t.Properties.Where(p => p.RiveProperty.Type == RiveDataType.List).Select(p => (t, p)))
            .First();
        var owner = type.CreateInstance();
        var list = (RiveList)owner.GetObject(property.Name)!;

        list.Add(context.ViewModels[property.RiveProperty.ViewModelReferenceId].CreateInstance());

        Assert.That(list.ToString(), Does.Contain("1"));
    }
}