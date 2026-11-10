using VL.Core;
using VL.Core.Import;
using VL.Rive.Interop;
using VL.Rive.Internal;

namespace VL.Rive;

/// <summary>
/// Offers the view models of the Rive file that is linked to the <c>File</c> input of the node.
/// </summary>
[ChoiceProviderRequires("File")]
[Smell(SymbolSmell.Hidden)]
public sealed class RiveViewModelChoiceProvider : IChoiceProvider
{
    /// <summary>How long to wait for the file to be there before giving up.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
        => RiveChoices.ReadAsync(context, "File", input => GetChoices(RiveChoices.Alive(input)), Timeout, cancellationToken);

    // Null (a plain text input) while there is no file
    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveContext? context)
        => context?.ViewModels.Select(type => new ChoiceEntry(type.Name)).ToList();
}

/// <summary>
/// Offers the instances that are defined in the file for the view model that is named by the <c>Name</c> input of the
/// node, in the file that is linked to its <c>File</c> input. The first one is the default instance, and no name at all
/// makes an empty instance.
/// </summary>
[Smell(SymbolSmell.Hidden)]
[ChoiceProviderRequires(FilePin, NamePin)]
public sealed class RiveViewModelInstanceChoiceProvider : IChoiceProvider
{
    // The pins are found by the name they are shown with, a space between the words
    private const string FilePin = "File";
    private const string NamePin = "Name";

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
        => RiveChoices.RunAsync(context, Timeout, cancellationToken, async token =>
        {
            var file = await context.ReadInputAsync(FilePin, input => input as RiveContext, token).ConfigureAwait(false);
            // Read on the thread of the runtime, where it is safe to ask the file for its view models
            return await context.ReadInputAsync(NamePin, name => GetChoices(file is { IsDisposed: false } ? file.FindViewModel(name as string ?? string.Empty) : null), token).ConfigureAwait(false);
        });

    // Null (a plain text input) while there is no such view model. The first choice is no name at all, it makes an
    // empty instance. The default instance is marked as such.
    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveViewModelType? type)
        => type is null
            ? null
            : type.InstanceNames
                .Select((name, index) => RiveChoices.Entry(name, index == 0 ? "default" : null))
                .Prepend(RiveChoices.Entry(string.Empty, "new empty instance"))
                .ToList();
}

/// <summary>Offers the artboards of the Rive file that is linked to the <c>File</c> input of the node.</summary>
[ChoiceProviderRequires("File")]
[Smell(SymbolSmell.Hidden)]
public sealed class RiveArtboardChoiceProvider : IChoiceProvider
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
        => RiveChoices.ReadAsync(context, "File", input => GetChoices(RiveChoices.Alive(input)), Timeout, cancellationToken);

    // The view model of an artboard is the hint behind its name
    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveContext? context)
        => context?.RiveFile.Artboards.Select(artboard => RiveChoices.Entry(artboard.Name, artboard.DefaultViewModel)).ToList();
}

/// <summary>
/// Offers the scenes (state machines and animations) of the artboard that is named by the <c>Artboard Name</c> input
/// of the node, in the file that is linked to its <c>File</c> input.
/// </summary>
[Smell(SymbolSmell.Hidden)]
[ChoiceProviderRequires(FilePin, ArtboardNamePin)]
public sealed class RiveSceneChoiceProvider : IChoiceProvider
{
    // The pins are found by the name they are shown with, a space between the words
    private const string FilePin = "File";
    private const string ArtboardNamePin = "Artboard Name";

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
        => RiveChoices.RunAsync(context, Timeout, cancellationToken, async token =>
        {
            var file = await context.ReadInputAsync(FilePin, input => input as RiveContext, token).ConfigureAwait(false);
            // Read on the thread of the runtime, where it is safe to ask the file for the artboard
            return await context.ReadInputAsync(ArtboardNamePin, name => GetChoices(file?.GetArtboardInfo(name as string)), token).ConfigureAwait(false);
        });

    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveArtboardInfo? info)
    {
        if (info is null)
            return null;

        return info.Value.StateMachines.Select(s => s.Name).Concat(info.Value.Animations.Select(a => a.Name))
            .Distinct().Select(name => new ChoiceEntry(name)).ToList();
    }
}

internal static class RiveChoices
{
    /// <summary>The Rive file of an input value, null if there is none or it is gone: it is not safe to ask it anything then.</summary>
    public static RiveContext? Alive(object? input) => input is RiveContext { IsDisposed: false } context ? context : null;

    /// <summary>A choice that inserts the value and shows the hint behind it, which the editor shows separated and dimmed.</summary>
    public static ChoiceEntry Entry(string value, string? hint)
        => hint is null ? new ChoiceEntry(value) : new ChoiceEntry(value, value + ChoiceEntry.LabelSeparator + hint);

    public static Task<IReadOnlyList<ChoiceEntry>?> ReadAsync(
        IChoiceContext context, string pin, Func<object?, IReadOnlyList<ChoiceEntry>?> choices, TimeSpan timeout, CancellationToken cancellationToken)
        // Read at the moment the value is traced, on the thread of the runtime. The names are immutable.
        => RunAsync(context, timeout, cancellationToken, token => context.ReadInputAsync(pin, input => choices(input), token));

    public static async Task<IReadOnlyList<ChoiceEntry>?> RunAsync(
        IChoiceContext context, TimeSpan timeout, CancellationToken cancellationToken, Func<CancellationToken, Task<IReadOnlyList<ChoiceEntry>?>> read)
    {
        // Nothing runs, so there is nothing to ask
        if (!context.IsRunning)
            return null;

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            return await read(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The node that gives the value didn't run
            return null;
        }
    }
}