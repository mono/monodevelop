//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Represents the base class for all types of Shell "containers". Any class deriving from this class
    /// can contain other ShellObjects (e.g. ShellFolder, FileSystemKnownFolder, ShellLibrary, etc)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "This will complicate the class hierarchy and naming convention used in the Shell area")]
    public abstract class ShellContainer : ShellObject, IEnumerable<ShellObject>, IDisposable
    {

        #region Private Fields

        private IShellFolder desktopFolderEnumeration;
        private IShellFolder nativeShellFolder;

        #endregion

        #region Internal Properties

        internal IShellFolder NativeShellFolder
        {
            get
            {
                if (nativeShellFolder == null)
                {
                    Guid guid = new Guid(ShellIIDGuid.IShellFolder);
                    Guid handler = new Guid(ShellBHIDGuid.ShellFolderObject);

                    HResult hr = NativeShellItem.BindToHandler(
                        IntPtr.Zero, ref handler, ref guid, out nativeShellFolder);

                    if (CoreErrorHelper.Failed(hr))
                    {
                        string str = ShellHelper.GetParsingName(NativeShellItem);
                        if (str != null && str != Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                        {
                            throw new ShellException(hr);
                        }
                    }
                }

                return nativeShellFolder;
            }
        }

        #endregion

        #region Internal Constructor

        internal ShellContainer() { }

        internal ShellContainer(IShellItem2 shellItem) : base(shellItem) { }

        #endregion

        #region Disposable Pattern

        /// <summary>
        /// Release resources
        /// </summary>
        /// <param name="disposing"><B>True</B> indicates that this is being called from Dispose(), rather than the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (nativeShellFolder != null)
            {
                Marshal.ReleaseComObject(nativeShellFolder);
                nativeShellFolder = null;
            }

            if (desktopFolderEnumeration != null)
            {
                Marshal.ReleaseComObject(desktopFolderEnumeration);
                desktopFolderEnumeration = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region IEnumerable<ShellObject> Members

        /// <summary>
        /// Enumerates through contents of the ShellObjectContainer
        /// </summary>
        /// <returns>Enumerated contents</returns>
        public IEnumerator<ShellObject> GetEnumerator()
        {
            if (NativeShellFolder == null)
            {
                if (desktopFolderEnumeration == null)
                {
                    ShellNativeMethods.SHGetDesktopFolder(out desktopFolderEnumeration);
                }

                nativeShellFolder = desktopFolderEnumeration;
            }

            return new ShellFolderItems(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ShellFolderItems(this);
        }

        #endregion
    }
}
