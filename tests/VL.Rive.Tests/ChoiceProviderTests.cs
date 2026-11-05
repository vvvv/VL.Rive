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
        var name = update.GetParameters().Single(p => p.GetCustomAttribute<ChoiceProviderAttribute>() != null);
        Assert.That(name.Name, Is.EqualTo("name"));
        Assert.That(name.GetCustomAttribute<NodeLabelAttribute>(), Is.Not.Null, "the chosen name is the label of the node");
        Assert.That(name.GetCustomAttribute<ChoiceProviderAttribute>()!.ProviderType, Is.EqualTo(typeof(RiveViewModelChoiceProvider)));

        // The editor matches the pins the provider reads by name, so the node must have one
        foreach (var pin in ChoiceProviderRequiresAttribute.GetInputPins(typeof(RiveViewModelChoiceProvider)))
            Assert.That(update.GetParameters().Select(p => p.Name), Has.One.EqualTo(pin.ToLowerInvariant()).IgnoreCase, $"input {pin}");
    }

    [Test]
    public void EveryNodeHasThePinsItsChoiceProvidersRead()
    {
        var updates = typeof(RiveFile).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ProcessNodeAttribute>() != null)
            .SelectMany(t => t.GetMethods().Where(m => m.Name == "Update"))
            .Where(m => m.GetParameters().Any(p => p.GetCustomAttribute<ChoiceProviderAttribute>() != null))
            .ToList();
        Assert.That(updates.Select(m => m.DeclaringType!.Name), Is.SupersetOf(new[] { nameof(RiveViewModelNode), nameof(RiveArtboardNode), nameof(RiveSceneNode) }));

        // The editor matches the pins a provider reads by name, so the node must have them
        foreach (var update in updates)
            foreach (var parameter in update.GetParameters().Where(p => p.GetCustomAttribute<ChoiceProviderAttribute>() != null))
                foreach (var pin in ChoiceProviderRequiresAttribute.GetInputPins(parameter.GetCustomAttribute<ChoiceProviderAttribute>()!.ProviderType))
                    Assert.That(update.GetParameters().Select(p => p.Name), Has.One.EqualTo(pin).IgnoreCase, $"input {pin} of {update.DeclaringType!.Name}");
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
            Assert.That(pinName, Is.EqualTo("File").IgnoreCase);
            if (!neverReads)
                return Task.FromResult(read(input));

            // The value never arrives: the task only ends when it is cancelled
            var never = new TaskCompletionSource<T>();
            cancellationToken.Register(() => never.TrySetCanceled(cancellationToken));
            return never.Task;
        }
    }
}
