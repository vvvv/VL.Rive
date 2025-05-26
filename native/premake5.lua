workspace "vl-rive-runtime"
configurations { "Debug", "Release" }
platforms { "x64", "ARM64" }

RIVE_RUNTIME_DIR = "../submodules/rive-runtime"

project("rive")
kind("SharedLib")
language("C++")
cppdialect("C++17")
-- Remove the generic targetdir, set per-platform below
objdir("obj/%{cfg.platform}/%{cfg.buildcfg}")
staticruntime("off") -- /MD for dll
toolset("clang")

includedirs({ RIVE_RUNTIME_DIR .. "/include", RIVE_RUNTIME_DIR .. "/renderer/include" })
files({ RIVE_RUNTIME_DIR .. "/src/**.cpp", "RiveSharpInterop.cpp" })

-- this is building the actual rive library so it seems we need this here.
defines({ "_RIVE_INTERNAL_" })

filter("configurations:Debug")
defines({ "DEBUG" })
symbols("On")

filter("configurations:Release")
defines({ "RELEASE" })
defines({ "NDEBUG" })
optimize("Size")

filter("platforms:x64")
  architecture("x64")
  targetdir("runtimes/win-x64/native")

filter("platforms:ARM64")
  architecture("ARM64")
  targetdir("runtimes/win-arm64/native")

filter({})
