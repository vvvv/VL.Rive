using System.Collections.Immutable;

namespace VL.Rive.Interop;

internal record struct RiveArtboard(string Name, ImmutableArray<RiveStateMachine> StateMachines, ImmutableArray<RiveAnimation> Animations);
