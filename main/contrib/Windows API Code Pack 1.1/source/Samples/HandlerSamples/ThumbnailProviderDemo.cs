using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.ShellExtensions;

namespace HandlerSamples
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("HandlerSamples.XYZThumbnailer")]
    [Guid("38AA8375-27EC-4EAF-955E-D7BDA633069F")]
    [ThumbnailProvider("XYZThumbnailer", ".xyz", ThumbnailAdornment = ThumbnailAdornment.VideoSprockets)]
    public class ThumbnailProviderDemo : ThumbnailProvider, IThumbnailFromStream, IThumbnailFromFile
    {
        #region IThumbnailFromStream Members

        public Bitmap ConstructBitmap(Stream stream, int sideSize)
        {
            XyzFileDefinition file = new XyzFileDefinition(stream);

            using (MemoryStream mstream = new MemoryStream(Convert.FromBase64String(file.EncodedImage)))
            {
                return new Bitmap(mstream);
            }
        }

        #endregion

        #region IThumbnailFromFile Members

        public Bitmap ConstructBitmap(FileInfo info, int sideSize)
        {
            using (FileStream stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read))
            {
                return ConstructBitmap(stream, sideSize);
            }
        }

        #endregion
    }
}
