using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.VSWhere;
using Nuke.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
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
class Build : NukeBuild
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
    AbsolutePath W64DevKitExe => LocalBinDirectory / "w64devkit-x64-2.11.0.7z.exe";
    AbsolutePath W64DevKitDirectory => LocalBinDirectory / "w64devkit" / "bin";
    AbsolutePath Premake5Zip => LocalBinDirectory / "premake5.zip";
    AbsolutePath Premake5Exe => LocalBinDirectory / "premake5.exe";
    AbsolutePath Python3Zip => LocalBinDirectory / "python-3.13.17-embed-amd64.zip";
    AbsolutePath PythonExe => LocalBinDirectory / "python.exe";
    AbsolutePath Python3Exe => LocalBinDirectory / "python3.exe";
    AbsolutePath RiveBuildDirectory => RootDirectory / "submodules" / "rive-runtime" / "build";
    AbsolutePath PremakeOutputDirectory => BuildDirectory / "out" / "debug";
    // premake's vs2022 action writes rive.sln, vs2026 writes rive.slnx
    AbsolutePath RiveNativeSolution => (PremakeOutputDirectory / "rive.slnx").FileExists() ? PremakeOutputDirectory / "rive.slnx" : PremakeOutputDirectory / "rive.sln";
    AbsolutePath RiveManagedProject => RootDirectory / "src" / "VL.Rive.csproj";
    AbsolutePath PackageOutputDirectory => RootDirectory / "lib";

    // Latest Visual Studio 2022 (17.x) or 2026 (18.x) installation
    AbsolutePath VisualStudioInstallPath => _vsInstallPath ??= FindVisualStudio("installationPath");
    AbsolutePath _vsInstallPath;

    static string FindVisualStudio(string property)
    {
        var value = VSWhereTasks.VSWhere(s => s
            .SetVersion("[17.0,19.0)")
            .EnableLatest()
            .EnableUTF8()
            .SetProperty(property))
            .Output
            .FirstOrDefault()
            .Text;

        if (string.IsNullOrEmpty(value))
            throw new Exception("Visual Studio installation not found.");

        return value;
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            PremakeOutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore, BuildRiveNative, BuildRiveManaged)
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
            await DownloadFile("https://github.com/skeeto/w64devkit/releases/download/v2.11.0/w64devkit-x64-2.11.0.7z.exe", W64DevKitExe);
        });

    Target ExtractW64DevKit => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !W64DevKitDirectory.DirectoryExists())
        .DependsOn(DownloadW64DevKit, Download7Zip)
        .Executes(() =>
        {
            ProcessTasks.StartProcess(SevenZip, $"x -y {W64DevKitExe} -o{LocalBinDirectory}")
                .AssertWaitForExit()
                .AssertZeroExitCode();
        });

    Target DownloadPremake5 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Premake5Zip.FileExists())
        .Executes(async () =>
        {
            LocalBinDirectory.CreateDirectory();
            await DownloadFile("https://github.com/premake/premake-core/releases/download/v5.0.0-beta8/premake-5.0.0-beta8-windows.zip", Premake5Zip);
        });

    Target ExtractPremake5 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Premake5Exe.FileExists())
        .DependsOn(DownloadPremake5)
        .Executes(() =>
        {
            LocalBinDirectory.CreateDirectory();
            Premake5Zip.UnZipTo(LocalBinDirectory);
        });

    Target DownloadPython3 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Python3Zip.FileExists())
        .Executes(async () =>
        {
            LocalBinDirectory.CreateDirectory();
            await DownloadFile("https://www.python.org/ftp/python/3.13.17/python-3.13.17-embed-amd64.zip", Python3Zip);
        });

    Target ExtractPython3 => _ => _
        .Unlisted()
        .OnlyWhenDynamic(() => !Python3Exe.FileExists())
        .DependsOn(DownloadPython3)
        .Executes(() =>
        {
            LocalBinDirectory.CreateDirectory();
            Python3Zip.UnZipTo(LocalBinDirectory);
            PythonExe.Copy(Python3Exe, ExistsPolicy.FileOverwrite);
        });

    Target GenerateInteropSolution => _ => _
        .Unlisted()
        .DependsOn(ExtractPremake5, ExtractPython3, ExtractW64DevKit)
        .Executes(() =>
        {
            var vsInstallPath = VisualStudioInstallPath;

            var vcvarsPath = vsInstallPath / "VC" / "Auxiliary" / "Build" / "vcvarsall.bat";
            if (!vcvarsPath.FileExists())
                throw new Exception($"vcvarsall.bat not found at: {vcvarsPath}");

            var env = Environment.GetEnvironmentVariables();
            env["Path"] = string.Join(
                Path.PathSeparator,
                RiveBuildDirectory,
                LocalBinDirectory,
                W64DevKitDirectory,
                env["Path"]
            );

            var environmentVariables = env.Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value);

            // VS 2026 is 18.x, VS 2022 is 17.x
            var vsVersion = FindVisualStudio("installationVersion");
            var vsAction = vsVersion.StartsWith("18.") ? "vs2026" : "vs2022";
            Log.Information("Using Visual Studio {Version} at {Path} ({Action})", vsVersion, vsInstallPath, vsAction);

            var premakeArgs = $"{vsAction} --with_rive_text --with_rive_layout --with_rive_scripting";
            var cmd = $@"/c """"{vcvarsPath}"" x64 && ""{Premake5Exe}"" {premakeArgs}""";

            ProcessTasks.StartProcess("cmd.exe", cmd, workingDirectory: BuildDirectory, environmentVariables: environmentVariables)
                .AssertWaitForExit()
                .AssertZeroExitCode();
        });

    Target BuildRiveNative => _ => _
        .Unlisted()
        .DependsOn(GenerateInteropSolution)
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetProcessToolPath(VisualStudioInstallPath / "MSBuild" / "Current" / "Bin" / "MSBuild.exe")
                .SetSolutionFile(RiveNativeSolution)
                .SetTargetPlatform(MSBuildTargetPlatform.x64)
                // rive only enables /MP for the vs2022 premake action: compile files in parallel for vs2026 too
                .SetProcessAdditionalArguments("/m", "/p:UseMultiToolTask=true")
                .SetVerbosity(MSBuildVerbosity.Quiet));
        });

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
        .DependsOn(GenerateInteropCode, BuildRiveNative)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RiveManagedProject));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
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
