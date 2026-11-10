using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>
/// The native calls the renderer makes: the view model is bound to the artboard only, the scene is made from the
/// bound artboard and advanced. These run the calls headless, they do not tell what is drawn.
/// </summary>
[TestFixture]
public class ArtboardBindingTests
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
    public void ASceneMadeFromABoundArtboardKeepsRunningWhenAnotherInstanceReplacesTheFirst()
    {
        using var artboard = context.RiveFile.GetArtboardDefault();
        var type = context.ViewModels[artboard.ViewModelId];
        var first = type.CreateInstance();
        var second = type.CreateInstance();

        artboard.BindViewModelInstance(first);
        using var scene = artboard.GetDefaultScene()!;
        Assert.DoesNotThrow(() => scene.AdvanceAndApply(1f / 60));

        // The state machine shares the data context of the artboard, so it follows the new instance
        Assert.DoesNotThrow(() => artboard.BindViewModelInstance(second));
        Assert.DoesNotThrow(() => scene.AdvanceAndApply(1f / 60));

        // And back to the first, with the second still alive
        Assert.DoesNotThrow(() => artboard.BindViewModelInstance(first));
        Assert.DoesNotThrow(() => scene.AdvanceAndApply(1f / 60));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void WhatThePointerChangesInTheSceneIsToldByTheBoundInstance(bool defaultInstance)
    {
        using var artboard = context.RiveFile.GetArtboardDefault();
        var type = context.ViewModels[artboard.ViewModelId];
        var instance = defaultInstance ? type.CreateDefaultInstance() : type.CreateInstance();
        var changed = new HashSet<string>();
        instance.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        artboard.BindViewModelInstance(instance);
        using var scene = artboard.GetDefaultScene()!;
        scene.AdvanceAndApply(1f / 60);
        instance.AcknowledgeChanges();
        changed.Clear();

        // Like the frames of a node: pointer input, advance, acknowledge
        var bounds = artboard.Bounds;
        for (var ix = 1; ix < 12; ix++)
        for (var iy = 1; iy < 12; iy++)
        {
            var x = bounds.minX + (bounds.maxX - bounds.minX) * ix / 12f;
            var y = bounds.minY + (bounds.maxY - bounds.minY) * iy / 12f;
            scene.PointerMove(x, y, 0, 0);
            scene.PointerDown(x, y, 0);
            scene.PointerUp(x, y, 0);
            scene.AdvanceAndApply(1f / 60);
            instance.AcknowledgeChanges();
        }

        Assert.That(changed, Does.Contain("Counter"));
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void AStateMachineFollowsTheInstanceOnlyIfTheArtboardWasBoundBeforeItWasMade(bool bindFirst, bool reacts)
    {
        // The health bar colors itself from the health: a change of the view model makes the state machine change healthColor
        using var factory2 = new RiveFactory();
        using var healthContext = new RiveContext(nodeContext: null, factory2.LoadFile(Path.Combine(AppContext.BaseDirectory, "assets", "24637-46037-health-bar-data-binding-quick-start.riv")));
        using var artboard = healthContext.RiveFile.GetArtboardDefault();
        var instance = healthContext.ViewModels[artboard.ViewModelId].CreateDefaultInstance();
        var changed = new HashSet<string>();
        instance.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        if (bindFirst)
            artboard.BindViewModelInstance(instance);
        using var scene = artboard.GetDefaultScene()!;
        if (!bindFirst)
            artboard.BindViewModelInstance(instance);
        scene.AdvanceAndApply(1f / 60);
        healthContext.AcknowledgeChanges();
        changed.Clear();

        instance.SetObject("gameOver", System.Reactive.Unit.Default);
        scene.AdvanceAndApply(1f / 60);
        scene.AdvanceAndApply(1f / 60);
        healthContext.AcknowledgeChanges();

        Assert.That(changed.Contains("healthColor"), Is.EqualTo(reacts));
    }

    [Test]
    public void EveryAnimationOfTheArtboardRunsWithABoundArtboard()
    {
        var info = context.RiveFile.Artboards.First(a => a.Animations.Length > 0);
        using var artboard = context.RiveFile.GetArtboard(info.Name)!;
        var instance = context.ViewModels[artboard.ViewModelId].CreateInstance();
        artboard.BindViewModelInstance(instance);

        foreach (var animation in info.Animations)
        {
            using var scene = artboard.GetScene(animation.Name)!;
            Assert.DoesNotThrow(() => scene.AdvanceAndApply(1f / 60), animation.Name);
        }
    }
}
