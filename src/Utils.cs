using System.Text;
using VL.Rive.Interop;
using Path = VL.Lib.IO.Path;

namespace VL.Rive;

public static class Utils
{
    public static string DumpViewModelAsJson(Path filePath)
    {
        using var factory = new RiveFactory();
        var file = factory.LoadFile(filePath);

        return DumpViewModelAsJson(file);
    }

    internal static string DumpViewModelAsJson(this RiveFile file)
    {
        var sb = new StringBuilder();
        foreach (var vm in file.ViewModels)
            vm.DumpAsJson(file.ViewModels, sb);

        return sb.ToString();
    }
}
