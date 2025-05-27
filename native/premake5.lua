RIVE_RUNTIME_DIR = path.getabsolute("../submodules/rive-runtime")
premake.path = premake.path .. ";" .. RIVE_RUNTIME_DIR .. "/build"

dofile(RIVE_RUNTIME_DIR .. "/premake5_v2.lua")
dofile(RIVE_RUNTIME_DIR .. "/renderer/premake5_pls_renderer.lua")
dofile(RIVE_RUNTIME_DIR .. '/decoders/premake5_v2.lua')

--workspace "vl-rive-runtime"
--configurations { "Debug", "Release" }
--platforms { "x64", "ARM64" }


project("vl-rive-interop")
dependson("rive")
dependson("rive-decoders")
dependson("rive-renderer-pls")
kind("SharedLib")
language("C++")
cppdialect("C++17")

links({
    'rive',
    'rive_pls_renderer',
    'rive_decoders',
    'libwebp',
    'rive_harfbuzz',
    'rive_sheenbidi',
    'rive_yoga',
})

links({ 'zlib', 'libpng' })
links({ 'libjpeg' })

links({ 'd3dcompiler' })
--links({ 'd3d11', 'dxguid', 'dxgi', 'd3dcompiler' })

-- Remove the generic targetdir, set per-platform below
objdir("obj/%{cfg.platform}/%{cfg.buildcfg}")
--staticruntime("off") -- /MD for dll
toolset("clang")

includedirs({ RIVE_RUNTIME_DIR .. "/include", RIVE_RUNTIME_DIR .. "/renderer/include" })
--files({ RIVE_RUNTIME_DIR .. "/src/**.cpp", RIVE_RUNTIME_DIR .. "/renderer/src/*.cpp", RIVE_RUNTIME_DIR .. "/renderer/src/d3d11/*.cpp", RIVE_RUNTIME_DIR .. "/renderer/src/d3d/*.cpp", "RiveSharpInterop.cpp" })
files({ "RiveSharpInterop.cpp" })

-- this is building the actual rive library so it seems we need this here.
--defines({ "_RIVE_INTERNAL_" })

--filter("configurations:Debug")
--defines({ "DEBUG" })
--symbols("On")

--filter("configurations:Release")
--defines({ "RELEASE" })
--defines({ "NDEBUG" })
--optimize("Size")

filter("platforms:x64")
  architecture("x64")
  targetdir("runtimes/win-x64/native")

filter("platforms:ARM64")
  architecture("ARM64")
  targetdir("runtimes/win-arm64/native")

filter({})
