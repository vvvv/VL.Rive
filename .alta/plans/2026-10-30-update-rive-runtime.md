---
title: Update the rive-runtime submodule to the latest upstream
status: in-progress
created: 2026-10-30
summary: Move `submodules/rive-runtime` from upstream commit 121dd6de (2025-11-18) to current upstream `main` (e54b5539, 987 commits later), and adapt the interop layer to the few API changes that affect it.
---

# Update the rive-runtime submodule to the latest upstream

`submodules/rive-runtime` is pinned to `121dd6de` (2025-11-18). Upstream `main` is now `e54b5539` (2026-10-30), **987 commits** ahead. Once done, VL.Rive builds against current Rive runtime code, with the native interop and the C# bindings in sync.

## Findings

- The vvvv fork (`vvvv/rive-runtime`) `main` is **identical to upstream at `121dd6de`**: it carries no patches. The SRGB patch lives only on the fork's `srgb` branch, and the project already reverted its use (`d135c02`). The `arm64-hack` branch is not used by `main`.
- So this is a pure upstream bump, not a rebase of fork patches.
- The diff is huge (4290 files), but most of it is a reformat (4-space to 2-space indent) plus new subsystems VL.Rive does not use (Ore GPU layer, scripting/Luau, canvas, KTX/ASTC decoders). The surface VL.Rive touches is small.

### What VL.Rive uses, and what changed

| Used by | Upstream change | Impact |
| --- | --- | --- |
| `RenderContext::FrameDescriptor` (`RiveSharpInterop.hpp`, mirrored by hand in `src/Interop/FrameDescriptor.cs`) | New fields: `ditherMode` (enum, default `interleavedGradientNoise`), `triangulationThresholds` (struct), `virtualTileWidth/Height`, `inkbleedOverride`; inserted in the middle of the struct | **Breaking**: the C# struct no longer matches the native layout. Fields after `disableRasterOrdering` (wireframe, fills/strokes disabled, clockwise override) would be read from wrong offsets |
| `RenderContextD3DImpl::MakeContext(device, ctx, D3DContextOptions)` and `makeRenderTarget(w, h)` | Same signatures | None |
| `RenderContext::flush({ .renderTarget })` | `FlushResources` gained `currentFrameNumber` / `safeFrameNumber` (default 0) | Compiles. Resource recycling is driven by these counters, so they should be set (see below) |
| `utils/no_op_factory.hpp`, `utils/factory_utils.hpp` | Headers moved to `include/utils/` | `#include "utils/..."` still resolves through `rive-runtime/include`. `utils/no_op_factory.cpp` is still where `build/premake5.lua` expects it |
| `Factory` | New virtuals with defaults (`ore()`, `renderContext()`, `supportsLayerMask()` ...) | None for `NoOpFactory` |
| View-model runtime (`viewmodel_instance_runtime.hpp`) | Additions only: `viewModelName()`, `propertyListIndex`, `propertyFont`, `propertyBlob` | None; optional new features |
| premake (`build/premake5.lua`) | Project names we link (`rive`, `rive_pls_renderer`, `rive_decoders`, `libwebp`, `libpng`, `zlib`, `libjpeg`, `rive_harfbuzz`, `rive_sheenbidi`, `rive_yoga`) all still exist; `--with_rive_text` and `--with_rive_layout` still exist | Likely fine, but only a build proves it |
| Generated bindings (`build/generated/*.cs`, from `build/generate.rsp` via ClangSharp) | Headers changed | Regenerate and review the diff |

I did not build anything. Where reading headers cannot settle it (premake linking, new third-party downloads at build time, ClangSharp parsing the new headers), the build is the check.

## What changes

### 1. Bump the submodule

```bash
cd submodules/rive-runtime
git fetch upstream main        # remote `upstream` = https://github.com/rive-app/rive-runtime
git checkout e54b553982606145f88f6379839da208a2f55a06
cd ../.. && git add submodules/rive-runtime
```

The submodule URL is the vvvv fork. Its `main` must contain the target commit, or the parent repo (and CI, which checks out submodules recursively) cannot fetch it. See the open decision.

### 2. Fix `FrameDescriptor`

`src/Interop/FrameDescriptor.cs` must mirror the new native layout exactly, in order:

```csharp
enum DitherMode { /* members and underlying type as in render_context.hpp */ }

struct TriangulationThresholds   // triangulation_controller.hpp: float minArea, size_t maxVerbs, ...
{
    public float MinArea;
    public nuint MaxVerbs;
    // ... remaining fields as declared upstream
}

struct FrameDescriptor
{
    public uint RenderTargetWidth, RenderTargetHeight;
    public LoadAction LoadAction;
    public uint ClearColor;
    public uint MsaaSampleCount;          // native is uint32_t; the C# field is int today
    public bool DisableRasterOrdering;
    public DitherMode DitherMode;
    public TriangulationThresholds TriangulationThresholds;
    public uint VirtualTileWidth, VirtualTileHeight;
    public bool Wireframe, FillsDisabled, StrokesDisabled, ClockwiseFillOverride, InkbleedOverride;
}
```

To catch a silent mismatch in future bumps, add `static_assert(sizeof(RenderContext::FrameDescriptor) == N)` plus `offsetof` checks in `RiveSharpInterop.cpp`, and a C# test asserting `Unsafe.SizeOf<FrameDescriptor>()` equals the same `N`.

### 3. Frame counters on flush

`rive_RenderContext_Flush` passes only `renderTarget`. Upstream now tracks resource lifetime with `currentFrameNumber` and `safeFrameNumber`. With both at 0, nothing may ever be marked safe to recycle, which can grow GPU memory over time. Keep a frame counter in the managed `RiveRenderContext` and pass it through:

```cpp
void rive_RenderContext_Flush(RenderContext* self, RenderTarget* rt, uint64_t frame, uint64_t safeFrame)
{
    self->flush({ .renderTarget = rt, .currentFrameNumber = frame, .safeFrameNumber = safeFrame });
}
```

`safeFrame` is `frame - N`, with N the number of frames in flight. Before choosing N, check what the upstream D3D11 path expects (D3D11 resources are tracked by the driver, so a small N may be enough). Mirror the signature in `RiveSharpInterop.hpp` and `RiveRenderContext.cs`.

### 4. Rebuild and regenerate

```powershell
./build.ps1 BuildRiveNative GenerateInteropCode BuildRiveManaged
```

Review the `build/generated/` diff: expect only changes caused by upstream header edits. Fix any `generate.rsp` remap (`-r` entries) that no longer matches a renamed or new type.

## Steps

- [x] 1. Resolve the fork question (see Decisions), then check out `e54b5539` in `submodules/rive-runtime`
- [x] 2. Build native (`BuildRiveNative`); fix premake or link errors in `build/premake5.lua` if any
- [x] 3. Update `FrameDescriptor.cs` to the new layout; add the `static_assert` and the size test
- [x] 4. Pass frame counters through `rive_RenderContext_Flush` (`.hpp`, `.cpp`, `RiveRenderContext.cs`)
- [x] 5. Run `GenerateInteropCode`; review and commit the `build/generated/` diff
- [x] 6. Build managed (`BuildRiveManaged`) and fix compile errors
- [ ] 7. Run `VL.Rive.vl` / a help patch with a real `.riv` file in vvvv: rendering, state machine inputs, data binding (lists, colors, nested view models), pointer input
- [ ] 8. Commit in the style of `7d47d61` ("Updates rive to latest upstream bits ...")

## How it is verified

- [x] `./build.ps1` completes: native `rive_interop.dll` links and the managed project compiles
- [x] The `FrameDescriptor` size/offset assertions pass (native and managed agree)
- [ ] A `.riv` file with a state machine and view model renders correctly in a vvvv help patch, including the `Wireframe` / `FillsDisabled` flags, which sit after the shifted fields and would break first
- [ ] Run for several minutes while watching GPU memory: it stays flat (frame counters)
- [ ] Rendering to a texture still has correct colors (the ARGB/sRGB handling from `c8fd4ac` and `d135c02`)

## Decisions

| Decision | Choice | Why |
| --- | --- | --- |
| Target commit | Upstream `main` at `e54b5539` | The request is "latest"; pinned by SHA, so it is reproducible |
| Scope of new features | Only make the existing interop work | New view-model properties (font, blob, list index), Ore/canvas and scripting are not needed for the update and can follow separately |
| Build options | Keep `--with_rive_text --with_rive_layout`; scripting and canvas stay off | Same feature set as today, no Luau/Ore build cost |
| Frame counters | Pass them (step 4) | Without them upstream resource recycling may never trigger |

> **Decided:** option (a), fast-forward the fork main. Original question: The submodule points at the vvvv fork, whose `main` is still at `121dd6de`. Either (a) fast-forward the fork's `main` to upstream (needs a push to `vvvv/rive-runtime`, which I will not do without your go-ahead), or (b) repoint `.gitmodules` to `rive-app/rive-runtime`, since the fork has no patches on `main`. I recommend (a): CI and URLs stay unchanged.

## Risks

- A 987-commit bump can bring new build requirements (clang version, new pinned third-party repos downloaded by premake, Windows SDK). Build early (step 2), before editing code.
- Rendering may differ slightly from today: dithering is on by default (`ditherMode`) and the triangulation heuristics are new. If the dither is unwanted, set the dither mode to none in the default descriptor.
- The `FrameDescriptor` mismatch fails silently (wrong flag values), not with a crash; hence the size/offset assertions.

## Out of scope

- Using new runtime features (fonts, blobs, list indexes, scripting, Ore canvas)
- The ARM64 build (`arm64-hack` branch) and the SRGB patch
- Changes to the vvvv node surface or help patches beyond what verification needs
