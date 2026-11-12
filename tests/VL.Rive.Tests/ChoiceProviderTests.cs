using System.Reflection;
using NUnit.Framework;
using VL.Core;
using VL.Core.Import;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>The view models the editor offers for the name input of the ViewModel node.</summary>
[TestFixture]
public class ChoiceProviderTests
{
    [Test]
    public void TheNodeOffersTheChoicesOnItsNamePinAndTheProviderReadsTheFilePin()
    {
        var update = typeof(RiveViewModelNode).GetMethod(nameof(RiveViewModelNode.Update))!;
        var name = update.GetParameters().Single(p => p.GetCustomAttribute<ChoiceProviderAttribute>()?.ProviderType == typeof(RiveViewModelChoiceProvider));
        Assert.That(name.Name, Is.EqualTo("name"));
        Assert.That(name.GetCustomAttribute<NodeLabelAttribute>(), Is.Not.Null, "the chosen name is the label of the node");
        Assert.That(name.GetCustomAttribute<ChoiceProviderAttribute>()!.ProviderType, Is.EqualTo(typeof(RiveViewModelChoiceProvider)));

        // The editor matches the pins the provider reads by the name of the pin as it is shown, so the node must have one
        foreach (var pin in ChoiceProviderRequiresAttribute.GetInputPins(typeof(RiveViewModelChoiceProvider)))
            Assert.That(update.GetParameters().Select(p => PinName(p.Name)), Has.One.EqualTo(pin).IgnoreCase, $"input {pin}");
    }

    /// <summary>The name of the pin that a parameter makes in vvvv: words are separated by a space, "artboardName" is "Artboard Name".</summary>
    private static string PinName(string parameterName)
    {
        var spaced = System.Text.RegularExpressions.Regex.Replace(parameterName, "(?<=[a-z0-9])(?=[A-Z])", " ");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    [Test]
    public void EveryNodeHasThePinsItsChoiceProvidersRead()
    {
        var updates = typeof(RiveContext).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ProcessNodeAttribute>() != null)
            .SelectMany(t => t.GetMethods().Where(m => m.Name == "Update"))
            .Where(m => m.GetParameters().Any(p => p.GetCustomAttribute<ChoiceProviderAttribute>() != null))
            .ToList();
        Assert.That(updates.Select(m => m.DeclaringType!.Name), Is.SupersetOf(new[] { nameof(RiveViewModelNode), nameof(RiveRenderer) }));

        // The editor matches the pins a provider reads by the name of the pin as it is shown (a space between the words),
        // so the node must have them. A pin that does not exist makes the provider fail, and the editor offers nothing.
        foreach (var update in updates)
            foreach (var parameter in update.GetParameters().Where(p => p.GetCustomAttribute<ChoiceProviderAttribute>() != null))
                foreach (var pin in ChoiceProviderRequiresAttribute.GetInputPins(parameter.GetCustomAttribute<ChoiceProviderAttribute>()!.ProviderType))
                    Assert.That(update.GetParameters().Select(p => PinName(p.Name)), Has.One.EqualTo(pin).IgnoreCase, $"input {pin} of {update.DeclaringType!.Name}");
    }

    [Test]
    public void EveryViewModelOfTheFileIsAChoice()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));

        var choices = RiveViewModelChoiceProvider.GetChoices(context)!;

        Assert.That(choices.Select(c => c.Value), Is.EqualTo(context.ViewModels.Select(t => t.Name)));
        Assert.That(choices, Is.Not.Empty);
    }

    [Test]
    public void EveryInstanceDefinedInTheFileIsAChoiceAndTheFirstIsTheDefaultOne()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));
        var type = context.ViewModels.First(t => t.InstanceNames.Length > 0);

        var choices = RiveViewModelInstanceChoiceProvider.GetChoices(type)!;

        // The first choice is no name, the ones behind it are the instances
        Assert.That(choices.Skip(1).Select(c => c.Value), Is.EqualTo(type.InstanceNames));
        Assert.That(choices[1].Label, Is.EqualTo(type.InstanceNames[0] + ChoiceEntry.LabelSeparator + "default"));
        Assert.That(choices.Skip(2).Select(c => c.Label), Is.All.Null);
    }

    [Test]
    public void TheFirstInstanceChoiceIsNoNameAtAllAndMakesAnEmptyInstance()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));

        // Also a view model without instances offers it, so there is always the way back to an empty instance
        foreach (var type in context.ViewModels)
        {
            var first = RiveViewModelInstanceChoiceProvider.GetChoices(type)![0];

            Assert.That(first.Value, Is.Empty, type.Name);
            Assert.That(first.Label, Is.EqualTo(ChoiceEntry.LabelSeparator + "new empty instance"), type.Name);
        }
        Assert.That(context.ViewModels.Any(t => t.InstanceNames.Length == 0), Is.True, "a view model without instances is part of the test");
    }

    [Test]
    public void ThereAreNoInstanceChoicesWithoutAViewModel()
    {
        Assert.That(RiveViewModelInstanceChoiceProvider.GetChoices(null), Is.Null);
    }

    [Test]
    public void AnArtboardChoiceNamesItsViewModel()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));

        var choices = RiveArtboardChoiceProvider.GetChoices(context)!;

        foreach (var (choice, artboard) in choices.Zip(context.RiveFile.Artboards))
        {
            Assert.That(choice.Value, Is.EqualTo(artboard.Name));
            if (artboard.DefaultViewModel is null)
                Assert.That(choice.Label, Is.Null);
            else
                Assert.That(choice.Label, Is.EqualTo(artboard.Name + ChoiceEntry.LabelSeparator + artboard.DefaultViewModel));
        }
        Assert.That(choices.Any(c => c.Label != null), Is.True, "at least one artboard has a view model");
    }

    [Test]
    public void EveryArtboardOfTheFileIsAChoice()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));

        var choices = RiveArtboardChoiceProvider.GetChoices(context)!;

        Assert.That(choices.Select(c => c.Value), Is.EqualTo(context.RiveFile.Artboards.Select(a => a.Name)));
        Assert.That(choices, Is.Not.Empty);
    }

    [Test]
    public void ThereAreNoArtboardOrSceneChoicesWithoutAFile()
    {
        Assert.That(RiveArtboardChoiceProvider.GetChoices(null), Is.Null);
        Assert.That(RiveSceneChoiceProvider.GetChoices(null), Is.Null);
    }

    [Test]
    public void EveryStateMachineAndAnimationOfTheArtboardIsAChoice()
    {
        using var factory = new RiveFactory();
        using var context = new RiveContext(nodeContext: null, factory.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "data-binding-demo.riv")));
        var info = context.RiveFile.Artboards.First();

        var choices = RiveSceneChoiceProvider.GetChoices(info)!;

        Assert.That(choices.Select(c => c.Value), Is.EquivalentTo(info.StateMachines.Select(s => s.Name).Concat(info.Animations.Select(a => a.Name)).Distinct()));
    }

    [Test]
    public void ThereAreNoChoicesWithoutAFile()
    {
        Assert.That(RiveViewModelChoiceProvider.GetChoices(null), Is.Null);
    }

    [Test]
    public async Task ThereAreNoChoicesWhileNothingRuns()
    {
        var choices = await new RiveViewModelChoiceProvider().GetChoicesAsync(new FakeContext(isRunning: false, input: null), CancellationToken.None);

        Assert.That(choices, Is.Null);
    }

    [Test]
    public async Task ThereAreNoChoicesForSomethingThatIsNotAFile()
    {
        var choices = await new RiveViewModelChoiceProvider().GetChoicesAsync(new FakeContext(isRunning: true, input: "not a file"), CancellationToken.None);

        Assert.That(choices, Is.Null);
    }

    [Test]
    public async Task ThereAreNoChoicesIfTheFileNeverComes()
    {
        var provider = new RiveViewModelChoiceProvider { Timeout = TimeSpan.FromMilliseconds(50) };

        var choices = await provider.GetChoicesAsync(new FakeContext(isRunning: true, input: null, neverReads: true), CancellationToken.None);

        Assert.That(choices, Is.Null);
    }

    private sealed class FakeContext(bool isRunning, object? input, bool neverReads = false) : IChoiceContext
    {
        public TypeRegistry TypeRegistry => throw new NotSupportedException();

        public bool IsRunning => isRunning;

        public Task<T> ReadInstanceAsync<T>(Func<object?, T> read, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<T> ReadInputAsync<T>(string pinName, Func<object?, T> read, CancellationToken cancellationToken = default)
        {
            Assert.That(pinName, Is.EqualTo("Input").IgnoreCase);
            if (!neverReads)
                return Task.FromResult(read(input));

            // The value never arrives: the task only ends when it is cancelled
            var never = new TaskCompletionSource<T>();
            cancellationToken.Register(() => never.TrySetCanceled(cancellationToken));
            return never.Task;
        }
    }
}