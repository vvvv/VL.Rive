using Fallout.Common;
using Fallout.Common.CI;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Solutions;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.MSBuild;
using Fallout.Common.Tools.VSWhere;
using Fallout.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Fallout.Common.EnvironmentInfo;
using static Fallout.Common.IO.PathConstruction;
using static System.Net.WebRequestMethods;

[GitHubActions(
    "main",
    GitHubActionsImage.WindowsLatest,
    //OnPushBranches = ["main"],
    On = [GitHubActionsTrigger.WorkflowDispatch],
    Lfs = true,
    Submodules = GitHubActionsSubmodules.Recursive,
    InvokedTargets = [nameof(PushToGithub)],
    EnableGitHubToken = true)]
[GitHubActions(
    "nuget.org",
    GitHubActionsImage.WindowsLatest,
    On = [ GitHubActionsTrigger.WorkflowDispatch ],
    Lfs = true,
    Submodules = GitHubActionsSubmodules.Recursive,
    InvokedTargets = [nameof(PushToNugetOrg)],
    ImportSecrets = [nameof(VvvvOrgNugetKey)])]
class Build : FalloutBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter]
    [Secret]
    readonly string VvvvOrgNugetKey;

    AbsolutePath BuildDirectory => RootDirectory / "build";
    AbsolutePath LocalBinDirectory => BuildDirectory / "bin";
    AbsolutePath SevenZip => LocalBinDirectory / "7zr.exe";
    // Tools are unpacked into directories named after their version: bumping a version below
    // makes the build fetch the new one instead of silently reusing an older, cached copy.
    const string W64DevKitVersion = "2.11.0";
    const string Premake5Version = "5.0.0-beta8";
    const string Python3Version = "3.13.17";

    AbsolutePath W64DevKitExe => LocalBinDirectory / $"w64devkit-x64-{W64DevKitVersion}.7z.exe";
    AbsolutePath W64DevKitRoot => LocalBinDirectory / $"w64devkit-{W64DevKitVersion}";
    AbsolutePath W64DevKitDirectory => W64DevKitRoot / "w64devkit" / "bin";
    AbsolutePath Premake5Zip => LocalBinDirectory / $"premake-{Premake5Version}-windows.zip";
    AbsolutePath Premake5Directory => LocalBinDirectory / $"premake-{Premake5Version}";
    AbsolutePath Premake5Exe => Premake5Directory / "premake5.exe";
    AbsolutePath Python3Zip => LocalBinDirectory / $"python-{Python3Version}-embed-amd64.zip";
    AbsolutePath Python3Directory => LocalBinDirectory / $"python-{Python3Version}";
    AbsolutePath PythonExe => Python3Directory / "python.exe";
    AbsolutePath Python3Exe => Python3Directory / "python3.exe";
    AbsolutePath RiveBuildDirectory => RootDirectory / "submodules" / "rive-runtime" / "build";
    // ARM64 gets its own folder: objects and static libs of the two architectures must not mix
    static string PremakeOutputName(bool arm64) => arm64 ? "debug-arm64" : "debug";
    AbsolutePath PremakeOutputDirectory(bool arm64) => BuildDirectory / "out" / PremakeOutputName(arm64);
    AbsolutePath RiveNativeSolution(bool arm64) => PremakeOutputDirectory(arm64) / "rive.slnx";
    AbsolutePath RiveManagedProject => RootDirectory / "src" / "VL.Rive.csproj";
    AbsolutePath PackageOutputDirectory => RootDirectory / "lib";

    // Local builds produce the native runtime of the machine they run on; Pack builds both
    // Local builds produce the native runtime matching the architecture of the dotnet running the build (a native
    // ARM64 dotnet on an ARM64 machine). Pack builds both; BuildRiveNativeArm64 can always be requested explicitly.
    static bool HostIsArm64 => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    // Latest Visual Studio 2026 (18.x) installation
    AbsolutePath VisualStudioInstallPath => _vsInstallPath ??= FindVisualStudioInstallPath();
    AbsolutePath _vsInstallPath;

    static string FindVisualStudioInstallPath()
    {
        var value = VSWhereTasks.VSWhere(s => s
            .SetVersion("[18.0,19.0)")
            .EnableLatest()
            .EnableUTF8()
            .SetProperty("installationPath"))
            .Output
            .FirstOrDefault()
            .Text;

        if (string.IsNullOrEmpty(value))
            throw new Exception("Visual Studio 2026 installation not found.");

        return value;
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            PremakeOutputDirectory(arm64: false).CreateOrCleanDirectory();
            PremakeOutputDirectory(arm64: true).CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore, BuildRiveManaged)
        .Executes(() =>
        {
        });

    Target Download7Zip => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !SevenZip.FileExists())
        .Executes(async () =>
        {
            LocalBinDirectory.CreateDirectory();
            await DownloadFile("https://www.7-zip.org/a/7zr.exe", SevenZip);
        });

    Target DownloadW64DevKit => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !W64DevKitExe.FileExists())
        .Executes(async () =>
        {
            LocalBinDirectory.CreateDirectory();
            await DownloadFile($"https://github.com/skeeto/w64devkit/releases/download/v{W64DevKitVersion}/w64devkit-x64-{W64DevKitVersion}.7z.exe", W64DevKitExe);
        });

    Target ExtractW64DevKit => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !W64DevKitDirectory.DirectoryExists())
        .DependsOn(DownloadW64DevKit, Download7Zip)
        .Executes(() =>
        {
            ProcessTasks.StartProcess(SevenZip, $"x -y {W64DevKitExe} -o{W64DevKitRoot}")
                .AssertWaitForExit()
                .AssertZeroExitCode();
        });

    Target DownloadPremake5 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Premake5Zip.FileExists())
        .Executes(async () =>
        {
            LocalBinDirectory.CreateDirectory();
            await DownloadFile($"https://github.com/premake/premake-core/releases/download/v{Premake5Version}/premake-{Premake5Version}-windows.zip", Premake5Zip);
        });

    Target ExtractPremake5 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Premake5Exe.FileExists())
        .DependsOn(DownloadPremake5)
        .Executes(() =>
        {
            Premake5Zip.UnZipTo(Premake5Directory);
        });

    Target DownloadPython3 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Python3Zip.FileExists())
        .Executes(async () =>
        {
            LocalBinDirectory.CreateDirectory();
            await DownloadFile($"https://www.python.org/ftp/python/{Python3Version}/python-{Python3Version}-embed-amd64.zip", Python3Zip);
        });

    Target ExtractPython3 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Python3Exe.FileExists())
        .DependsOn(DownloadPython3)
        .Executes(() =>
        {
            Python3Zip.UnZipTo(Python3Directory);
            PythonExe.Copy(Python3Exe, ExistsPolicy.FileOverwrite);
        });

    void GenerateSolution(bool arm64)
    {
        var vsInstallPath = VisualStudioInstallPath;

        var vcvarsPath = vsInstallPath / "VC" / "Auxiliary" / "Build" / "vcvarsall.bat";
        if (!vcvarsPath.FileExists())
            throw new Exception($"vcvarsall.bat not found at: {vcvarsPath}");

        var env = Environment.GetEnvironmentVariables();
        env["Path"] = string.Join(
            Path.PathSeparator,
            RiveBuildDirectory,
            Python3Directory,
            W64DevKitDirectory,
            env["Path"]
        );

        var environmentVariables = env.Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value);

        var premakeArgs = $"vs2026 --with_rive_text --with_rive_layout --with_rive_scripting --out=out/{PremakeOutputName(arm64)}";
        var cmd = $@"/c """"{vcvarsPath}"" x64 && ""{Premake5Exe}"" {premakeArgs}""";

        ProcessTasks.StartProcess("cmd.exe", cmd, workingDirectory: BuildDirectory, environmentVariables: environmentVariables)
            .AssertWaitForExit()
            .AssertZeroExitCode();
    }

    void BuildNative(bool arm64)
    {
        MSBuildTasks.MSBuild(s => s
            .SetProcessToolPath(VisualStudioInstallPath / "MSBuild" / "Current" / "Bin" / "MSBuild.exe")
            .SetSolutionFile(RiveNativeSolution(arm64))
            .SetProperty("Platform", arm64 ? "ARM64" : "x64")
            // rive only enables /MP for premake's vs2022 action: compile files in parallel for vs2026 too
            .SetProcessAdditionalArguments("/m", "/p:UseMultiToolTask=true")
            .SetVerbosity(MSBuildVerbosity.Quiet));
    }

    Target GenerateInteropSolution => _ => _
        .Unlisted()
        .DependsOn(ExtractPremake5, ExtractPython3, ExtractW64DevKit)
        .Executes(() => GenerateSolution(arm64: false));

    Target GenerateInteropSolutionArm64 => _ => _
        .Unlisted()
        .DependsOn(ExtractPremake5, ExtractPython3, ExtractW64DevKit)
        .Executes(() => GenerateSolution(arm64: true));

    Target BuildRiveNative => _ => _
        .Unlisted()
        .DependsOn(GenerateInteropSolution)
        .Executes(() => BuildNative(arm64: false));

    // Cross-compiles the native runtime for Windows on ARM64 (runtimes/win-arm64). Part of Pack, not of Compile.
    Target BuildRiveNativeArm64 => _ => _
        .DependsOn(GenerateInteropSolutionArm64)
        .Executes(() => BuildNative(arm64: true));

    Target InstallClangSharpPInvokeGenerator => _ => _
        .Unlisted()
        .Executes(() =>
        {
            // Update (rather than install) so that a different, already installed version gets replaced
            DotNetTasks.DotNetToolUpdate(s => s
                .EnableGlobal()
                .SetPackageName("ClangSharpPInvokeGenerator")
                .SetVersion("21.1.8.4"));
        });

    Target GenerateInteropCode => _ => _
        .Unlisted()
        .DependsOn(InstallClangSharpPInvokeGenerator)
        .Executes(() =>
        {
            ProcessTasks.StartProcess("ClangSharpPInvokeGenerator", "@generate.rsp", workingDirectory: BuildDirectory)
                .AssertWaitForExit();
        });

    Target BuildRiveManaged => _ => _
        .Unlisted()
        .DependsOn(GenerateInteropCode, HostIsArm64 ? BuildRiveNativeArm64 : BuildRiveNative)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RiveManagedProject));
        });

    Target Pack => _ => _
        .DependsOn(Compile, BuildRiveNative, BuildRiveNativeArm64)
        .Executes(() =>
        {
            DotNetTasks.DotNetPack(s => s
                .SetProject(RiveManagedProject));
        });

    Target PushToGithub => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            foreach (var file in PackageOutputDirectory.GetFiles("*.nupkg"))
            {
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetTargetPath(file)
                    .SetApiKey(GitHubActions.Instance.Token)
                    .SetSource("https://nuget.pkg.github.com/vvvv/index.json"));
            }
        });

    Target PushToNugetOrg => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            foreach (var file in PackageOutputDirectory.GetFiles("*.nupkg"))
            {
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetTargetPath(file)
                    .SetApiKey(VvvvOrgNugetKey)
                    .SetSource("nuget.org"));
            }
        });

    private static async Task DownloadFile(string url, string file)
    {
        using var client = new HttpClient();
        using var downloadStream = await client.GetStreamAsync(url);
        using var fileStream = System.IO.File.OpenWrite(file);
        await downloadStream.CopyToAsync(fileStream);
    }
}
