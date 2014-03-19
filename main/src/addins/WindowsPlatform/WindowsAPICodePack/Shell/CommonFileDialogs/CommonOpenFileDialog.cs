//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Creates a Vista or Windows 7 Common File Dialog, allowing the user to select one or more files.
    /// </summary>
    /// 
    public sealed class CommonOpenFileDialog : CommonFileDialog
    {
        private NativeFileOpenDialog openDialogCoClass;

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommonOpenFileDialog()
            : base()
        {
            // For Open file dialog, allow read only files.
            base.EnsureReadOnly = true;
        }

        /// <summary>
        /// Creates a new instance of this class with the specified name.
        /// </summary>
        /// <param name="name">The name of this dialog.</param>
        public CommonOpenFileDialog(string name)
            : base(name)
        {
            // For Open file dialog, allow read only files.
            base.EnsureReadOnly = true;
        }

        #region Public API specific to Open

        /// <summary>
        /// Gets a collection of the selected file names.
        /// </summary>
        /// <remarks>This property should only be used when the
        /// <see cref="CommonOpenFileDialog.Multiselect"/>
        /// property is <b>true</b>.</remarks>
        public IEnumerable<string> FileNames
        {
            get
            {
                CheckFileNamesAvailable();
                return base.FileNameCollection;
            }
        }

        /// <summary>
        /// Gets a collection of the selected items as ShellObject objects.
        /// </summary>
        /// <remarks>This property should only be used when the
        /// <see cref="CommonOpenFileDialog.Multiselect"/>
        /// property is <b>true</b>.</remarks>
        public ICollection<ShellObject> FilesAsShellObject
        {
            get
            {
                // Check if we have selected files from the user.              
                CheckFileItemsAvailable();

                // temp collection to hold our shellobjects
                ICollection<ShellObject> resultItems = new Collection<ShellObject>();

                // Loop through our existing list of filenames, and try to create a concrete type of
                // ShellObject (e.g. ShellLibrary, FileSystemFolder, ShellFile, etc)
                foreach (IShellItem si in items)
                {
                    resultItems.Add(ShellObjectFactory.Create(si));
                }

                return resultItems;
            }
        }


        private bool multiselect;
        /// <summary>
        /// Gets or sets a value that determines whether the user can select more than one file.
        /// </summary>
        public bool Multiselect
        {
            get { return multiselect; }
            set { multiselect = value; }
        }

        private bool isFolderPicker;
        /// <summary>
        /// Gets or sets a value that determines whether the user can select folders or files.
        /// Default value is false.
        /// </summary>
        public bool IsFolderPicker
        {
            get { return isFolderPicker; }
            set { isFolderPicker = value; }
        }

        private bool allowNonFileSystem;
        /// <summary>
        /// Gets or sets a value that determines whether the user can select non-filesystem items, 
        /// such as <b>Library</b>, <b>Search Connectors</b>, or <b>Known Folders</b>.
        /// </summary>
        public bool AllowNonFileSystemItems
        {
            get { return allowNonFileSystem; }
            set { allowNonFileSystem = value; }
        }
        #endregion

        internal override IFileDialog GetNativeFileDialog()
        {
            Debug.Assert(openDialogCoClass != null, "Must call Initialize() before fetching dialog interface");

            return (IFileDialog)openDialogCoClass;
        }

        internal override void InitializeNativeFileDialog()
        {
            if (openDialogCoClass == null)
            {
                openDialogCoClass = new NativeFileOpenDialog();
            }
        }

        internal override void CleanUpNativeFileDialog()
        {
            if (openDialogCoClass != null)
            {
                Marshal.ReleaseComObject(openDialogCoClass);
            }
        }

        internal override void PopulateWithFileNames(Collection<string> names)
        {
            IShellItemArray resultsArray;
            uint count;

            openDialogCoClass.GetResults(out resultsArray);
            resultsArray.GetCount(out count);
            names.Clear();
            for (int i = 0; i < count; i++)
            {
                names.Add(GetFileNameFromShellItem(GetShellItemAt(resultsArray, i)));
            }
        }

        internal override void PopulateWithIShellItems(Collection<IShellItem> items)
        {
            IShellItemArray resultsArray;
            uint count;

            openDialogCoClass.GetResults(out resultsArray);
            resultsArray.GetCount(out count);
            items.Clear();
            for (int i = 0; i < count; i++)
            {
                items.Add(GetShellItemAt(resultsArray, i));
            }
        }

        internal override ShellNativeMethods.FileOpenOptions GetDerivedOptionFlags(ShellNativeMethods.FileOpenOptions flags)
        {
            if (multiselect)
            {
                flags |= ShellNativeMethods.FileOpenOptions.AllowMultiSelect;
            }
            if (isFolderPicker)
            {
                flags |= ShellNativeMethods.FileOpenOptions.PickFolders;
            }
            
            if (!allowNonFileSystem)
            {
                flags |= ShellNativeMethods.FileOpenOptions.ForceFilesystem;
            }
            else if (allowNonFileSystem)
            {
                flags |= ShellNativeMethods.FileOpenOptions.AllNonStorageItems;
            }

            return flags;
        }
    }
}
