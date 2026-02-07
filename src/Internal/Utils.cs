using CommunityToolkit.HighPerformance.Buffers;
using System.Collections.Immutable;
using System.Text;
using VL.Rive.Interop;
using Path = VL.Lib.IO.Path;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal static class Utils
{
    internal static unsafe RiveFile LoadFile(string path, nint factory)
    {
        // TODO: Check how it reacts if load fails
        using var fileStream = File.OpenRead(path);
        using var bytes = MemoryOwner<byte>.Allocate((int)fileStream.Length);
        fileStream.Read(bytes.Span);
        fixed (byte* p = bytes.Span)
        {
            RiveImportResult importResult;
            var riveFileHandle = rive_File_Import(p, bytes.Length, factory, &importResult);
            if (riveFileHandle == default)
                throw new FileLoadException($"Failed to load Rive file: {importResult}", path);

            return new RiveFile(riveFileHandle, factory);
        }
    }

    public static string DumpRiveFileAsJson(Path filePath)
    {
        using var factory = new RiveFactory();
        var file = factory.LoadFile(filePath);

        var sb = new StringBuilder();
        file.WriteRiveFileAsJson(sb);
        return sb.ToString();
    }

    internal static void WriteRiveFileAsJson(this RiveFile file, StringBuilder sb)
    {
        sb.AppendLine("{");

        // Artboards
        sb.AppendLine("  \"Artboards\": [");
        for (int i = 0; i < file.Artboards.Length; i++)
        {
            var artboard = file.Artboards[i];
            sb.AppendArtboard(artboard, "  ");

            if (i < file.Artboards.Length - 1)
                sb.AppendLine(",");
        }
        sb.AppendLine("],");

        // View models
        sb.AppendLine("  \"ViewModels\": [");
        for (int i = 0; i < file.ViewModels.Length; i++)
        {
            var viewModel = file.ViewModels[i];
            sb.AppendViewModel(viewModel, file.ViewModels, indent: "  ");

            if (i < file.ViewModels.Length - 1)
                sb.AppendLine(",");
        }
        sb.AppendLine("]");

        sb.AppendLine("}");
    }

    internal static void AppendViewModel(this StringBuilder sb, RiveViewModelInfo viewModel, ImmutableArray<RiveViewModelInfo> viewModels, string indent = "")
    {
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  \"Name\": \"{viewModel.Name}\",");
        sb.AppendLine($"{indent}  \"Properties\": [");
        for (int i = 0; i < viewModel.Properties.Length; i++)
        {
            var property = viewModel.Properties[i];
            var type = property.Type.ToString();
            if (property.Type == RiveDataType.ViewModel)
                type = viewModels.ElementAtOrDefault(property.ViewModelReferenceId).Name ?? "UnknownViewModel";
            sb.AppendLine($"{indent}    {{ \"Name\": \"{property.Name}\", \"Type\": \"{type}\" }}{(i < viewModel.Properties.Length - 1 ? "," : "")}");
        }
        sb.AppendLine($"{indent}  ]");
        sb.Append($"{indent}}}");
    }

    internal static void AppendArtboard(this StringBuilder sb, RiveArtboardInfo artboard, string indent = "")
    {
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  \"Name\": \"{artboard.Name}\",");

        if (artboard.DefaultViewModel is not null)
            sb.AppendLine($"{indent}  \"ViewModel\": \"{artboard.DefaultViewModel}\",");

        // State Machines
        sb.Append($"{indent}  \"StateMachines\": [ ");
        for (int i = 0; i < artboard.StateMachines.Length; i++)
        {
            var sm = artboard.StateMachines[i];
            sb.Append($"\"{sm.Name}\"");
            if (i < artboard.StateMachines.Length - 1)
                sb.Append(", ");
        }
        sb.AppendLine($" ]");

        // Animations
        sb.Append($"{indent}  \"Animations\": [ ");
        for (int i = 0; i < artboard.Animations.Length; i++)
        {
            var anim = artboard.Animations[i];
            sb.Append($"\"{anim.Name}\"");
            if (i < artboard.Animations.Length - 1)
                sb.Append(", ");
        }
        sb.AppendLine($" ]");

        sb.Append($"{indent}}}");
    }
}
