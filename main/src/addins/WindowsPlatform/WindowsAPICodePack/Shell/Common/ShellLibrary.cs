//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// A Shell Library in the Shell Namespace
    /// </summary>
    public sealed class ShellLibrary : ShellContainer, IList<ShellFileSystemFolder>
    {
        #region Private Fields

        private INativeShellLibrary nativeShellLibrary;
        private IKnownFolder knownFolder;

        private static Guid[] FolderTypesGuids = 
        {
            new Guid(ShellKFIDGuid.GenericLibrary),
            new Guid(ShellKFIDGuid.DocumentsLibrary),
            new Guid(ShellKFIDGuid.MusicLibrary),
            new Guid(ShellKFIDGuid.PicturesLibrary),
            new Guid(ShellKFIDGuid.VideosLibrary)
        };

        #endregion

        #region Private Constructor

        private ShellLibrary()
        {
            CoreHelpers.ThrowIfNotWin7();
        }

        //Construct the ShellLibrary object from a native Shell Library
        private ShellLibrary(INativeShellLibrary nativeShellLibrary)
            : this()
        {
            this.nativeShellLibrary = nativeShellLibrary;
        }

        /// <summary>
        /// Creates a shell library in the Libraries Known Folder, 
        /// using the given IKnownFolder
        /// </summary>
        /// <param name="sourceKnownFolder">KnownFolder from which to create the new Shell Library</param>
        /// <param name="isReadOnly">If <B>true</B> , opens the library in read-only mode.</param>
        private ShellLibrary(IKnownFolder sourceKnownFolder, bool isReadOnly)
            : this()
        {
            Debug.Assert(sourceKnownFolder != null);

            // Keep a reference locally
            knownFolder = sourceKnownFolder;

            nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();

            AccessModes flags = isReadOnly ?
                    AccessModes.Read :
                    AccessModes.ReadWrite;

            // Get the IShellItem2
            base.nativeShellItem = ((ShellObject)sourceKnownFolder).NativeShellItem2;

            Guid guid = sourceKnownFolder.FolderId;

            // Load the library from the IShellItem2
            try
            {
                nativeShellLibrary.LoadLibraryFromKnownFolder(ref guid, flags);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(LocalizedMessages.ShellLibraryInvalidLibrary, "sourceKnownFolder");
            }
            catch (NotImplementedException)
            {
                throw new ArgumentException(LocalizedMessages.ShellLibraryInvalidLibrary, "sourceKnownFolder");
            }
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Creates a shell library in the Libraries Known Folder, 
        /// using the given shell library name.
        /// </summary>
        /// <param name="libraryName">The name of this library</param>
        /// <param name="overwrite">Allow overwriting an existing library; if one exists with the same name</param>
        public ShellLibrary(string libraryName, bool overwrite)
            : this()
        {
            if (string.IsNullOrEmpty(libraryName))
            {
                throw new ArgumentException(LocalizedMessages.ShellLibraryEmptyName, "libraryName");
            }

            this.Name = libraryName;
            Guid guid = new Guid(ShellKFIDGuid.Libraries);

            ShellNativeMethods.LibrarySaveOptions flags = overwrite ?
                    ShellNativeMethods.LibrarySaveOptions.OverrideExisting :
                    ShellNativeMethods.LibrarySaveOptions.FailIfThere;

            nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();
            nativeShellLibrary.SaveInKnownFolder(ref guid, libraryName, flags, out nativeShellItem);
        }

        /// <summary>
        /// Creates a shell library in a given Known Folder, 
        /// using the given shell library name.
        /// </summary>
        /// <param name="libraryName">The name of this library</param>
        /// <param name="sourceKnownFolder">The known folder</param>
        /// <param name="overwrite">Override an existing library with the same name</param>
        public ShellLibrary(string libraryName, IKnownFolder sourceKnownFolder, bool overwrite)
            : this()
        {
            if (string.IsNullOrEmpty(libraryName))
            {
                throw new ArgumentException(LocalizedMessages.ShellLibraryEmptyName, "libraryName");
            }

            knownFolder = sourceKnownFolder;

            this.Name = libraryName;
            Guid guid = knownFolder.FolderId;

            ShellNativeMethods.LibrarySaveOptions flags = overwrite ?
                    ShellNativeMethods.LibrarySaveOptions.OverrideExisting :
                    ShellNativeMethods.LibrarySaveOptions.FailIfThere;

            nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();
            nativeShellLibrary.SaveInKnownFolder(ref guid, libraryName, flags, out nativeShellItem);
        }

        /// <summary>
        /// Creates a shell library in a given local folder, 
        /// using the given shell library name.
        /// </summary>
        /// <param name="libraryName">The name of this library</param>
        /// <param name="folderPath">The path to the local folder</param>
        /// <param name="overwrite">Override an existing library with the same name</param>
        public ShellLibrary(string libraryName, string folderPath, bool overwrite)
            : this()
        {
            if (string.IsNullOrEmpty(libraryName))
            {
                throw new ArgumentException(LocalizedMessages.ShellLibraryEmptyName, "libraryName");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(LocalizedMessages.ShellLibraryFolderNotFound);
            }

            this.Name = libraryName;

            ShellNativeMethods.LibrarySaveOptions flags = overwrite ?
                    ShellNativeMethods.LibrarySaveOptions.OverrideExisting :
                    ShellNativeMethods.LibrarySaveOptions.FailIfThere;

            Guid guid = new Guid(ShellIIDGuid.IShellItem);

            IShellItem shellItemIn;
            ShellNativeMethods.SHCreateItemFromParsingName(folderPath, IntPtr.Zero, ref guid, out shellItemIn);

            nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();
            nativeShellLibrary.Save(shellItemIn, libraryName, flags, out nativeShellItem);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The name of the library, every library must 
        /// have a name
        /// </summary>
        /// <exception cref="COMException">Will throw if no Icon is set</exception>
        public override string Name
        {
            get
            {
                if (base.Name == null && NativeShellItem != null)
                {
                    base.Name = System.IO.Path.GetFileNameWithoutExtension(ShellHelper.GetParsingName(NativeShellItem));
                }

                return base.Name;
            }
        }

        /// <summary>
        /// The Resource Reference to the icon.
        /// </summary>
        public IconReference IconResourceId
        {
            get
            {
                string iconRef;
                nativeShellLibrary.GetIcon(out iconRef);
                return new IconReference(iconRef);
            }

            set
            {
                nativeShellLibrary.SetIcon(value.ReferencePath);
                nativeShellLibrary.Commit();
            }
        }

        /// <summary>
        /// One of predefined Library types
        /// </summary>
        /// <exception cref="COMException">Will throw if no Library Type is set</exception>
        public LibraryFolderType LibraryType
        {
            get
            {
                Guid folderTypeGuid;
                nativeShellLibrary.GetFolderType(out folderTypeGuid);

                return GetFolderTypefromGuid(folderTypeGuid);
            }

            set
            {
                Guid guid = FolderTypesGuids[(int)value];
                nativeShellLibrary.SetFolderType(ref guid);
                nativeShellLibrary.Commit();
            }
        }

        /// <summary>
        /// The Guid of the Library type
        /// </summary>
        /// <exception cref="COMException">Will throw if no Library Type is set</exception>
        public Guid LibraryTypeId
        {
            get
            {
                Guid folderTypeGuid;
                nativeShellLibrary.GetFolderType(out folderTypeGuid);

                return folderTypeGuid;
            }
        }

        private static LibraryFolderType GetFolderTypefromGuid(Guid folderTypeGuid)
        {
            for (int i = 0; i < FolderTypesGuids.Length; i++)
            {
                if (folderTypeGuid.Equals(FolderTypesGuids[i]))
                {
                    return (LibraryFolderType)i;
                }
            }
            throw new ArgumentOutOfRangeException("folderTypeGuid", LocalizedMessages.ShellLibraryInvalidFolderType);
        }

        /// <summary>
        /// By default, this folder is the first location 
        /// added to the library. The default save folder 
        /// is both the default folder where files can 
        /// be saved, and also where the library XML 
        /// file will be saved, if no other path is specified
        /// </summary>
        public string DefaultSaveFolder
        {
            get
            {
                Guid guid = new Guid(ShellIIDGuid.IShellItem);

                IShellItem saveFolderItem;

                nativeShellLibrary.GetDefaultSaveFolder(
                    ShellNativeMethods.DefaultSaveFolderType.Detect,
                    ref guid,
                    out saveFolderItem);

                return ShellHelper.GetParsingName(saveFolderItem);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!Directory.Exists(value))
                {
                    throw new DirectoryNotFoundException(LocalizedMessages.ShellLibraryDefaultSaveFolderNotFound);
                }

                string fullPath = new DirectoryInfo(value).FullName;

                Guid guid = new Guid(ShellIIDGuid.IShellItem);
                IShellItem saveFolderItem;

                ShellNativeMethods.SHCreateItemFromParsingName(fullPath, IntPtr.Zero, ref guid, out saveFolderItem);

                nativeShellLibrary.SetDefaultSaveFolder(
                    ShellNativeMethods.DefaultSaveFolderType.Detect,
                    saveFolderItem);

                nativeShellLibrary.Commit();
            }
        }

        /// <summary>
        /// Whether the library will be pinned to the 
        /// Explorer Navigation Pane
        /// </summary>
        public bool IsPinnedToNavigationPane
        {
            get
            {
                ShellNativeMethods.LibraryOptions flags = ShellNativeMethods.LibraryOptions.PinnedToNavigationPane;

                nativeShellLibrary.GetOptions(out flags);

                return (
                    (flags & ShellNativeMethods.LibraryOptions.PinnedToNavigationPane) ==
                    ShellNativeMethods.LibraryOptions.PinnedToNavigationPane);
            }
            set
            {
                ShellNativeMethods.LibraryOptions flags = ShellNativeMethods.LibraryOptions.Default;

                if (value)
                {
                    flags |= ShellNativeMethods.LibraryOptions.PinnedToNavigationPane;
                }
                else
                {
                    flags &= ~ShellNativeMethods.LibraryOptions.PinnedToNavigationPane;
                }

                nativeShellLibrary.SetOptions(ShellNativeMethods.LibraryOptions.PinnedToNavigationPane, flags);
                nativeShellLibrary.Commit();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Close the library, and release its associated file system resources
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        #endregion

        #region Internal Properties

        internal const string FileExtension = ".library-ms";

        internal override IShellItem NativeShellItem
        {
            get { return NativeShellItem2; }
        }

        internal override IShellItem2 NativeShellItem2
        {
            get { return nativeShellItem; }
        }

        #endregion

        #region Static Shell Library methods

        /// <summary>
        /// Get a the known folder FOLDERID_Libraries 
        /// </summary>
        public static IKnownFolder LibrariesKnownFolder
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return KnownFolderHelper.FromKnownFolderId(new Guid(ShellKFIDGuid.Libraries));
            }
        }

        /// <summary>
        /// Load the library using a number of options
        /// </summary>
        /// <param name="libraryName">The name of the library</param>
        /// <param name="isReadOnly">If <B>true</B>, loads the library in read-only mode.</param>
        /// <returns>A ShellLibrary Object</returns>
        public static ShellLibrary Load(string libraryName, bool isReadOnly)
        {
            CoreHelpers.ThrowIfNotWin7();

            IKnownFolder kf = KnownFolders.Libraries;
            string librariesFolderPath = (kf != null) ? kf.Path : string.Empty;

            Guid guid = new Guid(ShellIIDGuid.IShellItem);
            IShellItem nativeShellItem;
            string shellItemPath = System.IO.Path.Combine(librariesFolderPath, libraryName + FileExtension);
            int hr = ShellNativeMethods.SHCreateItemFromParsingName(shellItemPath, IntPtr.Zero, ref guid, out nativeShellItem);

            if (!CoreErrorHelper.Succeeded(hr))
                throw new ShellException(hr);

            INativeShellLibrary nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();
            AccessModes flags = isReadOnly ?
                    AccessModes.Read :
                    AccessModes.ReadWrite;
            nativeShellLibrary.LoadLibraryFromItem(nativeShellItem, flags);

            ShellLibrary library = new ShellLibrary(nativeShellLibrary);
            try
            {
                library.nativeShellItem = (IShellItem2)nativeShellItem;
                library.Name = libraryName;

                return library;
            }
            catch
            {
                library.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load the library using a number of options
        /// </summary>
        /// <param name="libraryName">The name of the library.</param>
        /// <param name="folderPath">The path to the library.</param>
        /// <param name="isReadOnly">If <B>true</B>, opens the library in read-only mode.</param>
        /// <returns>A ShellLibrary Object</returns>
        public static ShellLibrary Load(string libraryName, string folderPath, bool isReadOnly)
        {
            CoreHelpers.ThrowIfNotWin7();

            // Create the shell item path
            string shellItemPath = System.IO.Path.Combine(folderPath, libraryName + FileExtension);
            ShellFile item = ShellFile.FromFilePath(shellItemPath);

            IShellItem nativeShellItem = item.NativeShellItem;
            INativeShellLibrary nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();
            AccessModes flags = isReadOnly ?
                    AccessModes.Read :
                    AccessModes.ReadWrite;
            nativeShellLibrary.LoadLibraryFromItem(nativeShellItem, flags);

            ShellLibrary library = new ShellLibrary(nativeShellLibrary);
            try
            {
                library.nativeShellItem = (IShellItem2)nativeShellItem;
                library.Name = libraryName;

                return library;
            }
            catch
            {
                library.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load the library using a number of options
        /// </summary>
        /// <param name="nativeShellItem">IShellItem</param>
        /// <param name="isReadOnly">read-only flag</param>
        /// <returns>A ShellLibrary Object</returns>
        internal static ShellLibrary FromShellItem(IShellItem nativeShellItem, bool isReadOnly)
        {
            CoreHelpers.ThrowIfNotWin7();

            INativeShellLibrary nativeShellLibrary = (INativeShellLibrary)new ShellLibraryCoClass();

            AccessModes flags = isReadOnly ?
                    AccessModes.Read :
                    AccessModes.ReadWrite;

            nativeShellLibrary.LoadLibraryFromItem(nativeShellItem, flags);

            ShellLibrary library = new ShellLibrary(nativeShellLibrary);
            library.nativeShellItem = (IShellItem2)nativeShellItem;

            return library;
        }

        /// <summary>
        /// Load the library using a number of options
        /// </summary>
        /// <param name="sourceKnownFolder">A known folder.</param>
        /// <param name="isReadOnly">If <B>true</B>, opens the library in read-only mode.</param>
        /// <returns>A ShellLibrary Object</returns>
        public static ShellLibrary Load(IKnownFolder sourceKnownFolder, bool isReadOnly)
        {
            CoreHelpers.ThrowIfNotWin7();
            return new ShellLibrary(sourceKnownFolder, isReadOnly);
        }

        private static void ShowManageLibraryUI(ShellLibrary shellLibrary, IntPtr windowHandle, string title, string instruction, bool allowAllLocations)
        {
            int hr = 0;

            Thread staWorker = new Thread(() =>
            {
                hr = ShellNativeMethods.SHShowManageLibraryUI(
                    shellLibrary.NativeShellItem,
                    windowHandle,
                    title,
                    instruction,
                    allowAllLocations ?
                       ShellNativeMethods.LibraryManageDialogOptions.NonIndexableLocationWarning :
                       ShellNativeMethods.LibraryManageDialogOptions.Default);
            });

            staWorker.SetApartmentState(ApartmentState.STA);
            staWorker.Start();
            staWorker.Join();

            if (!CoreErrorHelper.Succeeded(hr)) { throw new ShellException(hr); }
        }

        /// <summary>
        /// Shows the library management dialog which enables users to mange the library folders and default save location.
        /// </summary>
        /// <param name="libraryName">The name of the library</param>
        /// <param name="folderPath">The path to the library.</param>
        /// <param name="windowHandle">The parent window,or IntPtr.Zero for no parent</param>
        /// <param name="title">A title for the library management dialog, or null to use the library name as the title</param>
        /// <param name="instruction">An optional help string to display for the library management dialog</param>
        /// <param name="allowAllLocations">If true, do not show warning dialogs about locations that cannot be indexed</param>
        /// <remarks>If the library is already open in read-write mode, the dialog will not save the changes.</remarks>
        public static void ShowManageLibraryUI(string libraryName, string folderPath, IntPtr windowHandle, string title, string instruction, bool allowAllLocations)
        {
            // this method is not safe for MTA consumption and will blow
            // Access Violations if called from an MTA thread so we wrap this
            // call up into a Worker thread that performs all operations in a
            // single threaded apartment
            using (ShellLibrary shellLibrary = ShellLibrary.Load(libraryName, folderPath, true))
            {
                ShowManageLibraryUI(shellLibrary, windowHandle, title, instruction, allowAllLocations);
            }
        }

        /// <summary>
        /// Shows the library management dialog which enables users to mange the library folders and default save location.
        /// </summary>
        /// <param name="libraryName">The name of the library</param>
        /// <param name="windowHandle">The parent window,or IntPtr.Zero for no parent</param>
        /// <param name="title">A title for the library management dialog, or null to use the library name as the title</param>
        /// <param name="instruction">An optional help string to display for the library management dialog</param>
        /// <param name="allowAllLocations">If true, do not show warning dialogs about locations that cannot be indexed</param>
        /// <remarks>If the library is already open in read-write mode, the dialog will not save the changes.</remarks>
        public static void ShowManageLibraryUI(string libraryName, IntPtr windowHandle, string title, string instruction, bool allowAllLocations)
        {
            // this method is not safe for MTA consumption and will blow
            // Access Violations if called from an MTA thread so we wrap this
            // call up into a Worker thread that performs all operations in a
            // single threaded apartment
            using (ShellLibrary shellLibrary = ShellLibrary.Load(libraryName, true))
            {
                ShowManageLibraryUI(shellLibrary, windowHandle, title, instruction, allowAllLocations);
            }
        }

        /// <summary>
        /// Shows the library management dialog which enables users to mange the library folders and default save location.
        /// </summary>
        /// <param name="sourceKnownFolder">A known folder.</param>
        /// <param name="windowHandle">The parent window,or IntPtr.Zero for no parent</param>
        /// <param name="title">A title for the library management dialog, or null to use the library name as the title</param>
        /// <param name="instruction">An optional help string to display for the library management dialog</param>
        /// <param name="allowAllLocations">If true, do not show warning dialogs about locations that cannot be indexed</param>
        /// <remarks>If the library is already open in read-write mode, the dialog will not save the changes.</remarks>
        public static void ShowManageLibraryUI(IKnownFolder sourceKnownFolder, IntPtr windowHandle, string title, string instruction, bool allowAllLocations)
        {
            // this method is not safe for MTA consumption and will blow
            // Access Violations if called from an MTA thread so we wrap this
            // call up into a Worker thread that performs all operations in a
            // single threaded apartment
            using (ShellLibrary shellLibrary = ShellLibrary.Load(sourceKnownFolder, true))
            {
                ShowManageLibraryUI(shellLibrary, windowHandle, title, instruction, allowAllLocations);
            }
        }

        #endregion

        #region Collection Members

        /// <summary>
        /// Add a new FileSystemFolder or SearchConnector
        /// </summary>
        /// <param name="item">The folder to add to the library.</param>
        public void Add(ShellFileSystemFolder item)
        {
            if (item == null) { throw new ArgumentNullException("item"); }

            nativeShellLibrary.AddFolder(item.NativeShellItem);
            nativeShellLibrary.Commit();
        }

        /// <summary>
        /// Add an existing folder to this library
        /// </summary>
        /// <param name="folderPath">The path to the folder to be added to the library.</param>
        public void Add(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(LocalizedMessages.ShellLibraryFolderNotFound);
            }

            Add(ShellFileSystemFolder.FromFolderPath(folderPath));
        }

        /// <summary>
        /// Clear all items of this Library 
        /// </summary>
        public void Clear()
        {
            List<ShellFileSystemFolder> list = ItemsList;
            foreach (ShellFileSystemFolder folder in list)
            {
                nativeShellLibrary.RemoveFolder(folder.NativeShellItem);
            }

            nativeShellLibrary.Commit();
        }

        /// <summary>
        /// Remove a folder or search connector
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><B>true</B> if the item was removed.</returns>
        public bool Remove(ShellFileSystemFolder item)
        {
            if (item == null) { throw new ArgumentNullException("item"); }

            try
            {
                nativeShellLibrary.RemoveFolder(item.NativeShellItem);
                nativeShellLibrary.Commit();
            }
            catch (COMException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Remove a folder or search connector
        /// </summary>
        /// <param name="folderPath">The path of the item to remove.</param>
        /// <returns><B>true</B> if the item was removed.</returns>
        public bool Remove(string folderPath)
        {
            ShellFileSystemFolder item = ShellFileSystemFolder.FromFolderPath(folderPath);
            return Remove(item);
        }

        #endregion

        #region Disposable Pattern

        /// <summary>
        /// Release resources
        /// </summary>
        /// <param name="disposing">Indicates that this was called from Dispose(), rather than from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (nativeShellLibrary != null)
            {
                Marshal.ReleaseComObject(nativeShellLibrary);
                nativeShellLibrary = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Release resources
        /// </summary>
        ~ShellLibrary()
        {
            Dispose(false);
        }

        #endregion

        #region Private Properties

        private List<ShellFileSystemFolder> ItemsList
        {
            get { return GetFolders(); }
        }

        private List<ShellFileSystemFolder> GetFolders()
        {
            List<ShellFileSystemFolder> list = new List<ShellFileSystemFolder>();
            IShellItemArray itemArray;

            Guid shellItemArrayGuid = new Guid(ShellIIDGuid.IShellItemArray);

            HResult hr = nativeShellLibrary.GetFolders(ShellNativeMethods.LibraryFolderFilter.AllItems, ref shellItemArrayGuid, out itemArray);

            if (!CoreErrorHelper.Succeeded(hr)) { return list; }

            uint count;
            itemArray.GetCount(out count);

            for (uint i = 0; i < count; ++i)
            {
                IShellItem shellItem;
                itemArray.GetItemAt(i, out shellItem);
                list.Add(new ShellFileSystemFolder(shellItem as IShellItem2));
            }

            if (itemArray != null)
            {
                Marshal.ReleaseComObject(itemArray);
                itemArray = null;
            }

            return list;
        }

        #endregion

        #region IEnumerable<ShellFileSystemFolder> Members

        /// <summary>
        /// Retrieves the collection enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        new public IEnumerator<ShellFileSystemFolder> GetEnumerator()
        {
            return ItemsList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Retrieves the collection enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ItemsList.GetEnumerator();
        }

        #endregion

        #region ICollection<ShellFileSystemFolder> Members


        /// <summary>
        /// Determines if an item with the specified path exists in the collection.
        /// </summary>
        /// <param name="fullPath">The path of the item.</param>
        /// <returns><B>true</B> if the item exists in the collection.</returns>
        public bool Contains(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentNullException("fullPath");
            }

            return ItemsList.Any(folder => string.Equals(fullPath, folder.Path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if a folder exists in the collection.
        /// </summary>
        /// <param name="item">The folder.</param>
        /// <returns><B>true</B>, if the folder exists in the collection.</returns>
        public bool Contains(ShellFileSystemFolder item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return ItemsList.Any(folder => string.Equals(item.Path, folder.Path, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region IList<FileSystemFolder> Members

        /// <summary>
        /// Searches for the specified FileSystemFolder and returns the zero-based index of the
        /// first occurrence within Library list.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns>The index of the item in the collection, or -1 if the item does not exist.</returns>
        public int IndexOf(ShellFileSystemFolder item)
        {
            return ItemsList.IndexOf(item);
        }

        /// <summary>
        /// Inserts a FileSystemFolder at the specified index.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="item">The FileSystemFolder to insert.</param>
        void IList<ShellFileSystemFolder>.Insert(int index, ShellFileSystemFolder item)
        {
            // Index related options are not supported by IShellLibrary doesn't support them.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes an item at the specified index.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        void IList<ShellFileSystemFolder>.RemoveAt(int index)
        {
            // Index related options are not supported by IShellLibrary doesn't support them.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the folder at the specified index
        /// </summary>
        /// <param name="index">The index of the folder to retrieve.</param>
        /// <returns>A folder.</returns>
        public ShellFileSystemFolder this[int index]
        {
            get { return ItemsList[index]; }
            set
            {
                // Index related options are not supported by IShellLibrary
                // doesn't support them.
                throw new NotImplementedException();
            }
        }
        #endregion

        #region ICollection<ShellFileSystemFolder> Members

        /// <summary>
        /// Copies the collection to an array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index in the array at which to start the copy.</param>
        void ICollection<ShellFileSystemFolder>.CopyTo(ShellFileSystemFolder[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The count of the items in the list.
        /// </summary>
        public int Count
        {
            get { return ItemsList.Count; }
        }

        /// <summary>
        /// Indicates whether this list is read-only or not.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        /// <summary>
        /// Indicates whether this feature is supported on the current platform.
        /// </summary>
        new public static bool IsPlatformSupported
        {
            get
            {
                // We need Windows 7 onwards ...
                return CoreHelpers.RunningOnWin7;
            }
        }
    }

}
