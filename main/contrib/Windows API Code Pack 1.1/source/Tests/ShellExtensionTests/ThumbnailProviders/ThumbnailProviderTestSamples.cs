using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.WindowsAPICodePack.ShellExtensions;

namespace Tests.ShellExtensions
{
    public class StreamThumbnailProviderTestSample : ThumbnailProvider, IThumbnailFromStream
    {
        #region IThumbnailFromStream Members

        public Bitmap ConstructBitmap(System.IO.Stream stream, int sideSize)
        {
            return new Bitmap(sideSize, sideSize);
        }

        #endregion
    }

    public class FileThumbnailProviderTestSample : ThumbnailProvider, IThumbnailFromFile
    {

        #region IThumbnailFromFile Members

        public Bitmap ConstructBitmap(System.IO.FileInfo info, int sideSize)
        {
            return new Bitmap(sideSize, sideSize);
        }

        #endregion
    }

    public class ItemThumbnailProviderTestSample : ThumbnailProvider, IThumbnailFromShellObject
    {
        #region IThumbnailFromShellObject Members

        public Bitmap ConstructBitmap(Microsoft.WindowsAPICodePack.Shell.ShellObject shellObject, int sideSize)
        {
            return new Bitmap(sideSize, sideSize);
        }

        #endregion
    }



}
