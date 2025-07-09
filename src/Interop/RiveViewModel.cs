using System.Collections.Immutable;
using System.Text;

namespace VL.Rive.Interop;

internal record struct RiveViewModel(string Name, ImmutableArray<PropertyData> Properties)
{
    public void DumpAsJson(ImmutableArray<RiveViewModel> viewModels, StringBuilder sb)
    {
        sb.AppendLine("{");
        sb.AppendLine($"  \"Name\": \"{Name}\",");
        sb.AppendLine("  \"Properties\": [");
        for (int i = 0; i < Properties.Length; i++)
        {
            var property = Properties[i];
            var type = property.Type.ToString();
            if (property.Type == RiveDataType.ViewModel)
                type = viewModels.ElementAtOrDefault(property.ViewModelReferenceId).Name ?? "UnknownViewModel";
            sb.AppendLine($"    {{ \"Name\": \"{property.Name}\", \"Type\": \"{type}\" }}{(i < Properties.Length - 1 ? "," : "")}");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");
    }
}