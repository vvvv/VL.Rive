using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>Advancing in fixed steps, so playback does not depend on the frame rate.</summary>
[TestFixture]
public class FixedStepTests
{
    static RiveScene OpenScene(RiveFactory factory, out RiveFile file, out RiveArtboardInstance artboard)
    {
        var path = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "assets"), "*.riv").First();
        file = factory.LoadFile(path);
        artboard = file.GetArtboardDefault();
        return artboard.GetDefaultScene()!;
    }

    [Test]
    public void TheRemainderIsKeptForTheNextCall()
    {
        using var factory = new RiveFactory();
        using var scene = OpenScene(factory, out var file, out var artboard);
        using (file) using (artboard)
        {
            var step = RiveScene.FixedStepSeconds;
            Assert.That(scene.AdvanceFixed(step * 0.5f), Is.EqualTo(0));
            Assert.That(scene.AdvanceFixed(step * 0.6f), Is.EqualTo(1));
            Assert.That(scene.AdvanceFixed(step * 2.5f), Is.EqualTo(2));
        }
    }

    [Test]
    public void ThePerFrameTimeDoesNotChangeTheNumberOfSteps()
    {
        using var factory = new RiveFactory();
        using var scene = OpenScene(factory, out var file, out var artboard);
        using (file) using (artboard)
        {
            var steps = 0;
            for (var i = 0; i < 60; i++)
                steps += scene.AdvanceFixed(1f / 60f);
            Assert.That(steps, Is.InRange(119, 120));
        }
    }

    [Test]
    public void AStallIsNotCaughtUp()
    {
        using var factory = new RiveFactory();
        using var scene = OpenScene(factory, out var file, out var artboard);
        using (file) using (artboard)
        {
            Assert.That(scene.AdvanceFixed(60f), Is.LessThanOrEqualTo(31));
            Assert.That(scene.AdvanceFixed(-1f), Is.LessThanOrEqualTo(1));
        }
    }
}
