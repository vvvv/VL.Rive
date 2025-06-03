# Building
- Download and extract https://github.com/skeeto/w64devkit/releases
- Download and copy premake5.exe to `w64devkit/bin` folder
- Install python3, let it add itself to `PATH` (or do it manually), navigate to its installation folder and copy `python.exe` to `python3.exe`
- Start VS developer command prompt
- Expand path, for example: `set PATH=%PATH%;C:\path\to\w64devkit\bin;C:\path\to\python3;C:\path\to\repo\submodules\rive-runtime\build`
- Make sure `w64devkit`, `python3` and `submodules/rive-runtime/build` are present in `PATH` (double check with `where` command, search for `premake5`, `make`, `sh`, `python3`, `fxc`, `build_rive`)
- `cd build`
- `premake5 vs2022 --with_rive_text --with_rive_layout`
- Open generated solution in Visual Studio and build it

# Generating the interop code
- `dotnet tool install --global ClangSharpPInvokeGenerator --version 20.1.2.1`
- `cd build`
- `ClangSharpPInvokeGenerator @generate.rsp`

# VL.NewLibrary.Template

- [ ] A clear and concise description of what this package is and does, also what problem it solves.
- [ ] In case this is a wrapper, links to original code and which version of it is used
- [ ] In case this is for a device/protocol, links to the device/protocol-specs
- [ ] Required dependencies/drivers to download and install in the getting started section below
- [ ] If available, links to documentation (other than helppatches), tutorial videos, blog posts, ...
- [ ] Note that you can also [include images](https://devblogs.microsoft.com/nuget/add-a-readme-to-your-nuget-package/#markdown-and-image-support)!
- [ ] Mention any limitations

For use with vvvv, the visual live-programming environment for .NET: http://vvvv.org

## Getting started
- Install as [described here](https://thegraybook.vvvv.org/reference/hde/managing-nugets.html) via commandline:

    `nuget install VL.NewLibrary.Template -pre`

- Usage examples and more information are included in the pack and can be found via the [Help Browser](https://thegraybook.vvvv.org/reference/hde/findinghelp.html)

## Contributing
- Report issues on [the vvvv forum](https://forum.vvvv.org/c/vvvv-gamma/28)
- For custom development requests, please [get in touch](mailto:devvvvs@vvvv.org)
- When making a pull-request, please make sure to read the general [guidelines on contributing to vvvv libraries](https://thegraybook.vvvv.org/reference/extending/contributing.html)

## Credits
Links to libraries this is based on

## Sponsoring
Development of this library was partially sponsored by:  
* 
