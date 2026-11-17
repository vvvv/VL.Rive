using System.Runtime.InteropServices;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Interop;

internal class RiveScene : RiveObject
{
    public unsafe RiveScene(nint handle) : base(handle) 
    {
        var namePtr = rive_Scene_Name(handle);
        Name = SpanExtensions.AsString(namePtr);
        NativeMemory.Free(namePtr);
    }

    public unsafe string Name { get; }

    public RiveAABB Bounds
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            return rive_Scene_Bounds(handle);
        }
    }

    /// <summary>The length of an animation, 0 for a static scene and -1 for a state machine.</summary>
    public float DurationSeconds
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsClosed, this);
            return rive_Scene_DurationSeconds(handle);
        }
    }

    public void AdvanceAndApply(float elapsedSeconds)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Scene_AdvanceAndApply(handle, elapsedSeconds);
    }

    /// <summary>The step <see cref="AdvanceFixed"/> advances by.</summary>
    internal const float FixedStepSeconds = 1f / 120f;

    // A frame longer than this is not caught up on: a stall (a breakpoint, a hitch) must not make the scene run many steps in one frame
    const float MaxFrameSeconds = 0.25f;

    float accumulatedSeconds;

    /// <summary>
    /// Advances in steps of <see cref="FixedStepSeconds"/> and keeps the remainder for the next call, so a state machine plays the same at any frame rate:
    /// https://rive.app/docs/runtimes/cpp/rendering-loop#fixed-timestep-accumulator
    /// </summary>
    /// <returns>The number of steps taken</returns>
    public int AdvanceFixed(float elapsedSeconds)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        accumulatedSeconds += Math.Clamp(elapsedSeconds, 0f, MaxFrameSeconds);
        var steps = 0;
        while (accumulatedSeconds >= FixedStepSeconds)
        {
            accumulatedSeconds -= FixedStepSeconds;
            rive_Scene_AdvanceAndApply(handle, FixedStepSeconds);
            steps++;
        }
        return steps;
    }

    /// <summary>Sets the point in time of an animation, 0 is its start and 1 its end. False for a state machine, which has no time to seek.</summary>
    public bool SeekNormalized(float position)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_SeekNormalized(handle, position) != 0;
    }

    public void Draw(RiveRenderer renderer)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        rive_Scene_Draw(handle, renderer.DangerousGetHandle());
    }

    public RiveHitResult PointerDown(float x, float y, int pointerId)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerDown(handle, x, y, pointerId);
    }

    public RiveHitResult PointerMove(float x, float y, float timeStamp, int pointerId)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerMove(handle, x, y, timeStamp, pointerId);
    }

    public RiveHitResult PointerUp(float x, float y, int pointerId)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerUp(handle, x, y, pointerId);
    }

    public RiveHitResult PointerExit(float x, float y, int pointerId)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        return rive_Scene_PointerExit(handle, x, y, pointerId);
    }

    protected override bool ReleaseHandle()
    {
        rive_Scene_Destroy(handle);
        return true;
    }
}