using System.Drawing;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// This interface exposes the <see cref="ConsructBitmap"/> function for initializing the 
    /// Thumbnail Provider with a <typeparamref name="Stream"/>.
    /// If this interfaces is not used, then the handler must opt out of process isolation.
    /// This interface can be used in conjunction with the other intialization interfaces,
    /// but only 1 will be accessed according to the priorities preset by the Windows Shell:
    /// <typeparamref name="IThumbnailFromStream"/>
    /// <typeparamref name="IThumbnailFromShellObject"/>
    /// <typeparamref name="IThumbnailFromFile"/>
    /// </summary>
    public interface IThumbnailFromStream
    {
        /// <summary>
        /// Provides the <typeparamref name="Stream"/> to the item from which a thumbnail should be created.
        /// <remarks>Only 32bpp bitmaps support adornments. 
        /// While 24bpp bitmaps will be displayed they will not display adornments.
        /// Additional guidelines for developing thumbnails can be found at http://msdn.microsoft.com/en-us/library/cc144115(v=VS.85).aspx
        /// </remarks>
        /// </summary>
        /// <param name="stream">Stream to initialize the thumbnail</param>
        /// <param name="sideSize">Square side dimension in which the thumbnail should fit; the thumbnail will be scaled otherwise.</param>
        /// <returns></returns>
        Bitmap ConstructBitmap(Stream stream, int sideSize);
    }

    /// <summary>
    /// This interface exposes the <see cref="ConsructBitmap"/> function for initializing the 
    /// Thumbnail Provider with a <typeparamref name="ShellObject"/>.
    /// This interface can be used in conjunction with the other intialization interfaces,
    /// but only 1 will be accessed according to the priorities preset by the Windows Shell:
    /// <typeparamref name="IThumbnailFromStream"/>
    /// <typeparamref name="IThumbnailFromShellObject"/>
    /// <typeparamref name="IThumbnailFromFile"/>
    /// </summary>
    public interface IThumbnailFromShellObject
    {
        /// <summary>
        /// Provides the <typeparamref name="ShellObject"/> to the item from which a thumbnail should be created.
        /// <remarks>Only 32bpp bitmaps support adornments. 
        /// While 24bpp bitmaps will be displayed they will not display adornments.
        /// Additional guidelines for developing thumbnails can be found at http://msdn.microsoft.com/en-us/library/cc144115(v=VS.85).aspx
        /// </remarks>
        /// </summary>
        /// <param name="shellObject">ShellObject to initialize the thumbnail</param>
        /// <param name="sideSize">Square side dimension in which the thumbnail should fit; the thumbnail will be scaled otherwise.</param>
        /// <returns>Generated thumbnail</returns>
        Bitmap ConstructBitmap(ShellObject shellObject, int sideSize);
    }

    /// <summary>
    /// This interface exposes the <see cref="ConsructBitmap"/> function for initializing the 
    /// Thumbnail Provider with file information.
    /// This interface can be used in conjunction with the other intialization interfaces,
    /// but only 1 will be accessed according to the priorities preset by the Windows Shell:
    /// <typeparamref name="IThumbnailFromStream"/>
    /// <typeparamref name="IThumbnailFromShellObject"/>
    /// <typeparamref name="IThumbnailFromFile"/>
    /// </summary>
    public interface IThumbnailFromFile
    {
        /// <summary>
        /// Provides the <typeparamref name="FileInfo"/> to the item from which a thumbnail should be created.
        /// <remarks>Only 32bpp bitmaps support adornments. 
        /// While 24bpp bitmaps will be displayed they will not display adornments.
        /// Additional guidelines for developing thumbnails can be found at http://msdn.microsoft.com/en-us/library/cc144115(v=VS.85).aspx
        /// </remarks>
        /// </summary>
        /// <param name="info">FileInfo to initialize the thumbnail</param>
        /// <param name="sideSize">Square side dimension in which the thumbnail should fit; the thumbnail will be scaled otherwise.</param>
        /// <returns>Generated thumbnail</returns>
        Bitmap ConstructBitmap(FileInfo info, int sideSize);
    }
}
