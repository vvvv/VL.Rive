using System.Collections.Immutable;

namespace VL.Rive.Interop;

internal record struct RiveEnumInfo(string Name, ImmutableArray<string> Values);
