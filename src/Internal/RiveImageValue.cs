using Stride.Graphics;
using VL.Lib.Basics.Imaging;
using VL.Rive.Interop;
using VL.Stride.Utils;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

internal sealed class RiveImageValue : RiveValue<IImage?>
{
    private readonly RiveContext context;

    public RiveImageValue(nint handle, RiveContext context) : base(handle, RiveDataType.AssetImage)
    {
        this.context = context;
    }

    public override unsafe IImage? TypedValue
    {
        get => null; // AssetImage is write-only in Rive API
        set
        {
            if (value is null)
            {
                rive_ViewModelInstanceAssetImageRuntime_SetValue(handle, default);
                return;
            }

            // Extract image data
            using var imageData = value.GetData();
            var bytes = imageData.Bytes.Span;

            // Rive only understands PNG/JPEG/WEBP
            using var pngByteStream = new MemoryStream();
            fixed (byte* p = bytes)
            {
                var image = Image.New(new ImageDescription()
                {
                    Width = value.Info.Width,
                    Height = value.Info.Height,
                    ArraySize = 1,
                    MipLevels = 1,
                    Depth = 1,
                    Dimension = TextureDimension.Texture2D,
                    Format = value.Info.Format.ToTexturePixelFormat(ColorSpace.Gamma)
                }, (nint)p);
                image.Save(pngByteStream, ImageFileType.Png);
            }

            fixed (byte* p = pngByteStream.GetBuffer())
            {
                // Decode image using factory
                var renderImageHandle = rive_Factory_DecodeImage(context.FactoryHandle, p, (int)pngByteStream.Length);

                try
                {
                    // Set the render image in Rive (this increases the ref count)
                    rive_ViewModelInstanceAssetImageRuntime_SetValue(handle, renderImageHandle);
                }
                finally
                {
                    // Release our reference since Rive now owns it (ref counted)
                    rive_RenderImage_Release(renderImageHandle);
                }
            }
        }
    }
}
