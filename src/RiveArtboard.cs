using VL.Rive.Interop;

namespace VL.Rive;

/// <summary>
/// An artboard of a <see cref="RiveFile"/>: what the file says about it, nothing that plays. Gives the view model
/// the artboard is made for, and instances to show it (<see cref="RiveArtboardInstance"/>).
/// </summary>
public sealed class RiveArtboard
{
    internal RiveArtboard(RiveFile file, string? requestedName, string name, int viewModelId)
    {
        File = file;
        RequestedName = requestedName;
        Name = name;
        ViewModelId = viewModelId;
    }

    internal RiveFile File { get; }

    /// <summary>The name of the artboard, also for the default one.</summary>
    public string Name { get; }

    /// <summary>The name asked for, null for the default artboard.</summary>
    internal string? RequestedName { get; }

    internal int ViewModelId { get; }

    /// <summary>True once the file is gone.</summary>
    internal bool IsDisposed => File.IsDisposed;

    /// <summary>What the file says about the artboard, null once it is gone.</summary>
    internal RiveArtboardInfo? GetInfo()
    {
        if (File.GetContextOrNull() is not { } context)
            return null;
        foreach (var info in context.RiveFile.Artboards)
            if (info.Name == Name)
                return info;
        return null;
    }

    internal RiveArtboardInstance CreateInstance() => File.CreateArtboardInstance(this);

    public override string ToString() => $"{Name} ({File})";
}
