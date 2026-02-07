using VL.Rive.Interop;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal sealed class RiveArtboardValue : RiveValue<string?>
{
    private readonly RiveContext context;

    public RiveArtboardValue(nint handle, RiveContext context) : base(handle, RiveDataType.Artboard)
    {
        this.context = context;
    }

    public override unsafe string? TypedValue
    {
        get => null; // Artboard is write-only in Rive API
        set
        {
            nint bindableArtboardHandle;

            if (string.IsNullOrEmpty(value))
            {
                // Use default artboard when value is null or empty
                bindableArtboardHandle = rive_File_BindableArtboardDefault(context.RiveFile.DangerousGetHandle());
            }
            else
            {
                // Get bindable artboard by name from the file
                using var marshaledName = new MarshaledString(value);
                bindableArtboardHandle = rive_File_BindableArtboardNamed(context.RiveFile.DangerousGetHandle(), marshaledName.Value);
            }
            
            try
            {
                // Set the bindable artboard in Rive (this increases the ref count)
                rive_ViewModelInstanceArtboardRuntime_SetValue(handle, bindableArtboardHandle);
            }
            finally
            {
                // Release our reference since Rive now owns it (ref counted)
                rive_BindableArtboard_Release(bindableArtboardHandle);
            }
        }
    }
}
