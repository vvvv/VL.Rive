using System.Collections.Immutable;

namespace VL.Rive.Interop;

internal record struct RiveArtboardInfo(
    string Name, 
    ImmutableArray<RiveStateMachineInfo> StateMachines, 
    ImmutableArray<RiveAnimationInfo> Animations,
    string? DefaultViewModel);
