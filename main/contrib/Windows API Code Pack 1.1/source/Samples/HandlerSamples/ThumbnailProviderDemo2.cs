using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.ShellExtensions;

namespace HandlerSamples
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("HandlerSamples.XYZThumbnailer2")]
    [Guid("AB21A65F-E3E1-4D08-9422-29C7AB1BE0A5")]
    [ThumbnailProvider("XYZThumbnailer2", ".xyz2", ThumbnailAdornment = ThumbnailAdornment.PhotoBorder, DisableProcessIsolation = true)]
    public class ThumbnailProviderDemo2 : ThumbnailProvider, IThumbnailFromFile, IThumbnailFromShellObject
    {
        #region IThumbnailFromFile Members

        public Bitmap ConstructBitmap(FileInfo info, int sideSize)
        {
            using (FileStream stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read))
            {
                XyzFileDefinition file = new XyzFileDefinition(stream);

                using (MemoryStream imageStream = new MemoryStream(Convert.FromBase64String(file.EncodedImage)))
                {
                    return new Bitmap(imageStream);
                }
            }
        }

        #endregion

        #region IThumbnailFromShellObject Members

        public Bitmap ConstructBitmap(Microsoft.WindowsAPICodePack.Shell.ShellObject shellObject, int sideSize)
        {
            using (FileStream stream = new FileStream(shellObject.ParsingName, FileMode.Open, FileAccess.Read))
            {
                XyzFileDefinition file = new XyzFileDefinition(stream);

                using (MemoryStream imageStream = new MemoryStream(Convert.FromBase64String(file.EncodedImage)))
                {
                    return (Bitmap)Image.FromStream(imageStream);
                }
            }
        }

        #endregion
    }
}
