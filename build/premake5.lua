REPO_ROOT_DIR = path.getabsolute("..")
VL_RIVE_SRC_DIR = REPO_ROOT_DIR .. "/src/Interop"
RIVE_RUNTIME_DIR = REPO_ROOT_DIR .. "/submodules/rive-runtime"
RIVE_RENDERER_DIR = REPO_ROOT_DIR .. "/submodules/rive-runtime/renderer"
premake.path = premake.path .. ";" .. RIVE_RUNTIME_DIR .. "/build"

-- rive's scripts still use deprecated premake API (flags {...}) because they have to work with the premake
-- version rive pins (beta7), which lacks the replacements. We can't fix that here, so keep its warnings
-- out of the build log while loading them; our own code below is still checked.
premake.api.deprecations("off")
dofile(RIVE_RUNTIME_DIR .. "/premake5_v2.lua")
dofile(RIVE_RUNTIME_DIR .. "/renderer/premake5_pls_renderer.lua")
dofile(RIVE_RUNTIME_DIR .. '/decoders/premake5_v2.lua')
premake.api.deprecations("on")

-- TODO: Rework this whole script so we can build for multiple platforms at once
--build_shaders(RIVE_RENDERER_DIR .. '/src/shaders', RIVE_RENDERER_DIR .. '/pls/generated'))

-- Windows on ARM64: rive does not support it upstream (build scripts pin architecture('x64') for
-- system:windows). Add an ARM64 platform and override the pin from here. These workspace-level
-- blocks come after the pinning ones, so they win; select it with MSBuild /p:Platform=ARM64.
workspace("rive")
  platforms({ "x64", "ARM64" })
  filter({ "system:windows", "platforms:ARM64" })
    architecture("ARM64")
  filter({})

-- These projects pin architecture('x64') themselves (project scope beats the workspace block above)
for _, name in ipairs({ "rive", "rive_pls_renderer" }) do
  project(name)
    filter({ "system:windows", "platforms:ARM64" })
      architecture("ARM64")
    filter({})
end

-- libwebp adds x86-only SSE flags for every windows clang build
project("libwebp")
  filter({ "system:windows", "platforms:ARM64" })
    removebuildoptions({ "-mssse3", "-msse4.1" })
  filter({})

project("rive_interop")
do
  dependson("rive")
  dependson("rive-decoders")
  dependson("rive-renderer-pls")
  kind("SharedLib")
  language("C++")
  cppdialect("C++17")
  exceptionhandling("CThrow")
  targetdir(REPO_ROOT_DIR .. "/runtimes/win-x64/native")

  links({
      'rive',
      'rive_pls_renderer',
      'rive_decoders',
      'libwebp',
      'rive_harfbuzz',
      'rive_sheenbidi',
      'rive_yoga',
  })

  -- Luau VM, needed because rive is built with --with_rive_scripting (script assets in .riv files)
  links({ 'luau_vm' })

  links({ 'zlib', 'libpng' })
  links({ 'libjpeg' })

  links({ 'd3dcompiler' })
  --links({ 'd3d11', 'dxguid', 'dxgi', 'd3dcompiler' })

  --staticruntime("off") -- /MD for dll
  toolset("clang")

  includedirs({ RIVE_RUNTIME_DIR .. "/include", RIVE_RUNTIME_DIR .. "/renderer/include" })

  files({ VL_RIVE_SRC_DIR .. "/*.cpp", VL_RIVE_SRC_DIR .. "/*.hpp", RIVE_RUNTIME_DIR .. "/utils/no_op_factory.cpp" })

  filter("platforms:x64")
    architecture("x64")
    targetdir(REPO_ROOT_DIR .. "/runtimes/win-x64/native")

  filter("platforms:ARM64")
    architecture("ARM64")
    targetdir(REPO_ROOT_DIR .. "/runtimes/win-arm64/native")

  filter({})
end

function build_shaders(shaders_dir, output_dir)
	-- body
    -- Minify and compile PLS shaders offline.
    pls_generated_headers = output_dir .. '/include'
    local pls_shaders_absolute_dir = path.getabsolute(pls_generated_headers .. '/generated/shaders')
    local nproc
    if os.host() == 'windows' then
        nproc = os.getenv('NUMBER_OF_PROCESSORS')
    elseif os.host() == 'macosx' then
        local handle = io.popen('sysctl -n hw.physicalcpu')
        nproc = handle:read('*a')
        handle:close()
    else
        local handle = io.popen('nproc')
        nproc = handle:read('*a')
        handle:close()
    end
    nproc = nproc:gsub('%s+', '') -- remove whitespace
    local python_ply = dependency.github('dabeaz/ply', '5c4dc94d4c6d059ec127ee1493c735963a5d2645')
    local makecommand = 'make -C '
        .. shaders_dir
        .. ' -j'
        .. nproc
        .. ' OUT='
        .. pls_shaders_absolute_dir

    local minify_flags = '-p ' .. python_ply .. '/src'
    newoption({
        trigger = 'raw_shaders',
        description = 'don\'t rename shader variables, or remove whitespace or comments',
    })
    if _OPTIONS['raw_shaders'] then
        minify_flags = minify_flags .. ' --human-readable'
    end

    makecommand = makecommand .. ' FLAGS="' .. minify_flags .. '"'

    if os.host() == 'macosx' then
        if _OPTIONS['os'] == 'ios' and _OPTIONS['variant'] == 'system' then
            makecommand = makecommand .. ' rive_pls_ios_metallib'
        elseif _OPTIONS['os'] == 'ios' and _OPTIONS['variant'] == 'emulator' then
            makecommand = makecommand .. ' rive_pls_ios_simulator_metallib'
        elseif _OPTIONS['os'] == 'ios' and _OPTIONS['variant'] == 'xros' then
            makecommand = makecommand .. ' rive_renderer_xros_metallib'
        elseif _OPTIONS['os'] == 'ios' and _OPTIONS['variant'] == 'xrsimulator' then
            makecommand = makecommand .. ' rive_renderer_xros_simulator_metallib'
        elseif _OPTIONS['os'] == 'ios' and _OPTIONS['variant'] == 'appletvos' then
            makecommand = makecommand .. ' rive_renderer_appletvos_metallib'
        elseif _OPTIONS['os'] == 'ios' and _OPTIONS['variant'] == 'appletvsimulator' then
            makecommand = makecommand .. ' rive_renderer_appletvsimulator_metallib'
        else
            makecommand = makecommand .. ' rive_pls_macosx_metallib'
        end
    end

    if _TARGET_OS == 'windows' then
        makecommand = makecommand .. ' d3d'
    end

    if _OPTIONS['with_vulkan'] or _OPTIONS['with-dawn'] or _OPTIONS['with-webgpu'] then
        makecommand = makecommand .. ' spirv'
    end

    function execute_and_check(cmd)
        print(cmd)
        if not os.execute(cmd) then
            error('\nError executing command:\n  ' .. cmd)
        end
    end

    -- Build shaders.
    execute_and_check(makecommand)

    return pls_shaders_absolute_dir
end
