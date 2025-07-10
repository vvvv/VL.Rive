using System.Collections.Immutable;

namespace VL.Rive.Interop;

internal record struct RiveViewModel(string Name, ImmutableArray<PropertyData> Properties);