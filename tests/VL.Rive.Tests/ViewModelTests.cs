using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>
/// Runs the view model layer without vvvv: files are imported through the no-op factory (no GPU) and the
/// Rive context is created without a VL node context, which is only needed when VL asks for type information.
/// </summary>
[TestFixture]
public class ViewModelTests
{
    static readonly string AssetDirectory = Path.Combine(AppContext.BaseDirectory, "assets");

    static string Asset(string name) => Path.Combine(AssetDirectory, name);

    static IEnumerable<string> AllAssets() => Directory.EnumerateFiles(AssetDirectory, "*.riv").Select(Path.GetFileName)!;

    static RiveContext Load(RiveFactory factory, string asset) => new RiveContext(nodeContext: null, factory.LoadFile(Asset(asset)));

    [Test]
    public void ImportsAndReadsViewModelTypes()
    {
        using var factory = new RiveFactory();
        using var context = Load(factory, "data-binding-demo.riv");

        Assert.That(context.ViewModels, Is.Not.Empty);
        Assert.That(context.ViewModels.Any(viewModel => viewModel.Properties.Length > 0), Is.True);
    }

    [Test]
    public void NumbersRoundTripThroughTheNativeViewModel()
    {
        using var factory = new RiveFactory();
        using var context = Load(factory, "data-binding-demo.riv");

        var viewModel = context.ViewModels
            .Where(t => t.Properties.Any(p => p.RiveProperty.Type == RiveDataType.Number))
            .Select(t => t.CreateDefaultInstance())
            .First();
        var number = viewModel.Type.Properties.First(p => p.RiveProperty.Type == RiveDataType.Number).Name;

        viewModel.SetObject(number, 42f);

        Assert.That(viewModel.GetObject(number), Is.EqualTo(42f));
    }

    // Traversing every property of every view model is what once crashed the runtime
    [TestCaseSource(nameof(AllAssets))]
    public void EveryAssetImportsAndItsViewModelsCanBeRead(string asset)
    {
        using var factory = new RiveFactory();
        using var context = Load(factory, asset);

        foreach (var type in context.ViewModels)
        {
            var viewModel = type.CreateDefaultInstance();
            foreach (var property in type.Properties)
                Assert.DoesNotThrow(() => viewModel.GetObject(property.Name), $"{asset}: {type}.{property.Name}");
        }
    }
}
