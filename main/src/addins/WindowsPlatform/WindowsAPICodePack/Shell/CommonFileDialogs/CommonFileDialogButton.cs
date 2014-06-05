//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;

namespace Microsoft.WindowsAPICodePack.Dialogs.Controls
{
    /// <summary>
    /// Creates the push button controls used by the Common File Dialog.
    /// </summary>
    public class CommonFileDialogButton : CommonFileDialogProminentControl
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public CommonFileDialogButton() : base(string.Empty) { }

        /// <summary>
        /// Initializes a new instance of this class with the text only.
        /// </summary>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogButton(string text) : base(text) { }

        /// <summary>
        /// Initializes a new instance of this class with the specified name and text.
        /// </summary>
        /// <param name="name">The name of this control.</param>
        /// <param name="text">The text to display for this control.</param>
        public CommonFileDialogButton(string name, string text) : base(name, text) { }

        /// <summary>
        /// Attach the PushButton control to the dialog object
        /// </summary>
        /// <param name="dialog">Target dialog</param>
        internal override void Attach(IFileDialogCustomize dialog)
        {
            Debug.Assert(dialog != null, "CommonFileDialogButton.Attach: dialog parameter can not be null");

            // Add a push button control
            dialog.AddPushButton(this.Id, this.Text);

            // Make this control prominent if needed
            if (IsProminent) { dialog.MakeProminent(this.Id); }

            // Sync unmanaged properties with managed properties
            SyncUnmanagedProperties();
        }

        /// <summary>
        /// Occurs when the user clicks the control. This event is routed from COM via the event sink.
        /// </summary>
        public event EventHandler Click = delegate { };
        internal void RaiseClickEvent()
        {
            // Make sure that this control is enabled and has a specified delegate
            if (Enabled) { this.Click(this, EventArgs.Empty); }
        }
    }
}