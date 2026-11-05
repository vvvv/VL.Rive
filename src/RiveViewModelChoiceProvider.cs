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
        => RiveChoices.ReadAsync(context, "File", input => GetChoices((input as RiveFile)?.GetContextOrNull()), Timeout, cancellationToken);

    // Null (a plain text input) while there is no file
    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveContext? context)
        => context?.ViewModels.Select(type => new ChoiceEntry(type.Name)).ToList();
}

/// <summary>Offers the artboards of the Rive file that is linked to the <c>File</c> input of the node.</summary>
[ChoiceProviderRequires("File")]
[Smell(SymbolSmell.Hidden)]
public sealed class RiveArtboardChoiceProvider : IChoiceProvider
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
        => RiveChoices.ReadAsync(context, "File", input => GetChoices((input as RiveFile)?.GetContextOrNull()), Timeout, cancellationToken);

    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveContext? context)
        => context?.RiveFile.Artboards.Select(artboard => new ChoiceEntry(artboard.Name)).ToList();
}

/// <summary>Offers the scenes (state machines and animations) of the Rive artboard that is linked to the <c>Artboard</c> input of the node.</summary>
[ChoiceProviderRequires("Artboard")]
[Smell(SymbolSmell.Hidden)]
public sealed class RiveSceneChoiceProvider : IChoiceProvider
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
        => RiveChoices.ReadAsync(context, "Artboard", input => GetChoices((input as RiveArtboard)?.GetInfo()), Timeout, cancellationToken);

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