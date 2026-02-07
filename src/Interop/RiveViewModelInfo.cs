using System.Collections.Immutable;

namespace VL.Rive.Interop;

internal record struct RiveViewModelInfo(string Name, ImmutableArray<RivePropertyInfo> Properties);