//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;

namespace Microsoft.WindowsAPICodePack.Dialogs.Controls
{
    /// <summary>
    /// Defines the label controls in the Common File Dialog.
    /// </summary>
    public class CommonFileDialogLabel : CommonFileDialogControl
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommonFileDialogLabel() { }

        /// <summary>
        /// Creates a new instance of this class with the specified text.
        /// </summary>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogLabel(string text) : base(text) { }

        /// <summary>
        /// Creates a new instance of this class with the specified name and text.
        /// </summary>
        /// <param name="name">The name of this control.</param>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogLabel(string name, string text) : base(name, text) { }

        /// <summary>
        /// Attach this control to the dialog object
        /// </summary>
        /// <param name="dialog">Target dialog</param>
        internal override void Attach(IFileDialogCustomize dialog)
        {
            Debug.Assert(dialog != null, "CommonFileDialog.Attach: dialog parameter can not be null");

            // Add a text control
            dialog.AddText(this.Id, this.Text);

            // Sync unmanaged properties with managed properties
            SyncUnmanagedProperties();
        }
    }
}