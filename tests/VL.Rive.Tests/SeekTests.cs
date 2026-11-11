using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>Scrubbing: setting the point in time of an animation by hand.</summary>
[TestFixture]
public class SeekTests
{
    [Test]
    public void AnAnimationCanBeSeekedAndAStateMachineCanNot()
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "assets");
        using var factory = new RiveFactory();
        var testedAnimation = false;
        var testedStateMachine = false;

        foreach (var path in Directory.GetFiles(assets, "*.riv"))
        {
            using var file = factory.LoadFile(path);
            foreach (var info in file.Artboards)
            {
                foreach (var animation in info.Animations)
                {
                    using var artboard = file.GetArtboard(info.Name)!;
                    using var scene = artboard.GetScene(animation.Name);
                    if (scene is null || scene.DurationSeconds <= 0)
                        continue;

                    Assert.That(scene.SeekNormalized(0f), Is.True);
                    Assert.That(scene.SeekNormalized(0.5f), Is.True);
                    Assert.That(scene.SeekNormalized(1f), Is.True);
                    // Out of range is clamped
                    Assert.That(scene.SeekNormalized(-3f), Is.True);
                    Assert.That(scene.SeekNormalized(7f), Is.True);
                    testedAnimation = true;
                }

                foreach (var stateMachine in info.StateMachines)
                {
                    using var artboard = file.GetArtboard(info.Name)!;
                    using var scene = artboard.GetScene(stateMachine.Name);
                    if (scene is null)
                        continue;

                    Assert.That(scene.SeekNormalized(0.5f), Is.False);
                    testedStateMachine = true;
                }
            }
        }

        if (!testedAnimation)
            Assert.Inconclusive("None of the test files has an animation with a duration.");
        Assert.That(testedStateMachine, Is.True);
    }
}