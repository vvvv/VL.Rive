using VL.Core.Import;
using VL.Rive.Internal;

namespace VL.Rive;

/// <summary>
/// Offers the view models of the Rive file that the renderer linked to the <c>Renderer</c> input of the node has loaded.
/// </summary>
[ChoiceProviderRequires("Renderer")]
public sealed class RiveViewModelChoiceProvider : IChoiceProvider
{
    /// <summary>How long to wait for the renderer to run before giving up.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

    public async Task<IReadOnlyList<ChoiceEntry>?> GetChoicesAsync(IChoiceContext context, CancellationToken cancellationToken)
    {
        // Nothing runs, so there is no file to ask
        if (!context.IsRunning)
            return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            // Read at the moment the renderer's value is traced, on the thread of the runtime. The names are immutable.
            return await context.ReadInputAsync("Renderer", input => GetChoices((input as RiveRenderer)?.RiveContext), timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The renderer didn't run
            return null;
        }
    }

    // Null (a plain text input) while there is no file
    internal static IReadOnlyList<ChoiceEntry>? GetChoices(RiveContext? context)
        => context?.ViewModels.Select(type => new ChoiceEntry(type.Name)).ToList();
}
