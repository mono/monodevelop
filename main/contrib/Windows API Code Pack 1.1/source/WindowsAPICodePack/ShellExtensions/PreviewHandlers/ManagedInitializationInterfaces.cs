using System.IO;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{        
    /// <summary>
    /// This interface exposes the <see cref="Load"/> function for initializing the 
    /// Preview Handler with a <typeparamref name="Stream"/>.
    /// This interface can be used in conjunction with the other intialization interfaces,
    /// but only 1 will be accessed according to the priorities preset by the Windows Shell:
    /// <typeparamref name="IPreviewFromStream"/>
    /// <typeparamref name="IPreviewFromShellObject"/>
    /// <typeparamref name="IPreviewFromFile"/>
    /// </summary>
    public interface IPreviewFromStream
    {
        /// <summary>
        /// Provides the <typeparamref name="Stream"/> to the item from which a preview should be created.        
        /// </summary>
        /// <param name="stream">Stream to the previewed file, this stream is only available in the scope of this method.</param>
        void Load(Stream stream);
    }

    /// <summary>
    /// This interface exposes the <see cref="Load"/> function for initializing the 
    /// Preview Handler with a <typeparamref name="FileInfo"/>.
    /// This interface can be used in conjunction with the other intialization interfaces,
    /// but only 1 will be accessed according to the priorities preset by the Windows Shell:
    /// <typeparamref name="IPreviewFromStream"/>
    /// <typeparamref name="IPreviewFromShellObject"/>
    /// <typeparamref name="IPreviewFromFile"/>
    /// </summary>
    public interface IPreviewFromFile
    {
        /// <summary>
        /// Provides the <typeparamref name="FileInfo"/> to the item from which a preview should be created.        
        /// </summary>
        /// <param name="info">File information to the previewed file.</param>
        void Load(FileInfo info);
    }

    /// <summary>
    /// This interface exposes the <see cref="Load"/> function for initializing the 
    /// Preview Handler with a <typeparamref name="ShellObject"/>.
    /// This interface can be used in conjunction with the other intialization interfaces,
    /// but only 1 will be accessed according to the priorities preset by the Windows Shell:
    /// <typeparamref name="IPreviewFromStream"/>
    /// <typeparamref name="IPreviewFromShellObject"/>
    /// <typeparamref name="IPreviewFromFile"/>
    /// </summary>
    public interface IPreviewFromShellObject
    {
        /// <summary>
        /// Provides the <typeparamref name="ShellObject"/> from which a preview should be created.        
        /// </summary>
        /// <param name="shellObject">ShellObject for the previewed file, this ShellObject is only available in the scope of this method.</param>
        void Load(ShellObject shellObject);
    }
}
