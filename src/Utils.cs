using System.Collections.Immutable;
using System.Text;
using VL.Rive.Interop;
using Path = VL.Lib.IO.Path;

namespace VL.Rive;

public static class Utils
{
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

    internal static void AppendViewModel(this StringBuilder sb, RiveViewModel viewModel, ImmutableArray<RiveViewModel> viewModels, string indent = "")
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

    internal static void AppendArtboard(this StringBuilder sb, RiveArtboard artboard, string indent = "")
    {
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  \"Name\": \"{artboard.Name}\",");

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
