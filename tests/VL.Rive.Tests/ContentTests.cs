using NUnit.Framework;
using VL.Rive.Internal;
using VL.Rive.Interop;

namespace VL.Rive.Tests;

/// <summary>What the Content output of a file tells: the public copies of what the file holds.</summary>
[TestFixture]
public class ContentTests
{
    [Test]
    public void EveryEnumPropertyNamesAnEnumOfTheFile()
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "assets");
        using var factory = new RiveFactory();
        var enumProperties = 0;

        foreach (var path in Directory.GetFiles(assets, "*.riv"))
        {
            var file = factory.LoadFile(path);
            var content = RiveFileContent.From(file);

            Assert.That(content.Enums.Count, Is.EqualTo(file.Enums.Length), path);
            var names = content.Enums.Select(e => e.Name).ToHashSet();
            foreach (var property in content.ViewModels.SelectMany(v => v.Properties))
            {
                if (property.Kind == RivePropertyKind.EnumType)
                {
                    enumProperties++;
                    Assert.That(property.EnumType, Is.Not.Null.And.Matches<string>(names.Contains), $"{path}: {property.Name}");
                }
                else
                    Assert.That(property.EnumType, Is.Null, $"{path}: {property.Name}");
            }
        }

        TestContext.Out.WriteLine($"{enumProperties} enum properties checked");
    }
}
