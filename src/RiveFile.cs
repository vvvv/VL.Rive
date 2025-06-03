using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RiveSharpInterop.Methods;

namespace VL.Rive;

internal class RiveFile : RiveObject
{
    public static RiveFile FromHandle(nint handle)
    {
        if (handle == nint.Zero)
            throw new ArgumentNullException(nameof(handle), "Rive file handle cannot be null.");
        return new RiveFile(handle);
    }

    public RiveFile(nint handle) : base(handle) { }

    public RiveArtboard GetArtboardDefault()
    {
        var artboardHandle = rive_File_GetArtboardDefault(handle);
        if (artboardHandle == nint.Zero)
            throw new InvalidOperationException("Failed to get default artboard from Rive file.");
        return new RiveArtboard(artboardHandle);
    }

    protected override bool ReleaseHandle()
    {
        rive_File_Destroy(handle);
        return true;
    }
}