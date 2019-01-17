//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.ComponentModel;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Data associated with <see cref="TaskDialog.Closing"/> event.
    /// </summary>
    public class TaskDialogClosingEventArgs : CancelEventArgs
    {
        private TaskDialogResult taskDialogResult;
        /// <summary>
        /// Gets or sets the standard button that was clicked.
        /// </summary>
        public TaskDialogResult TaskDialogResult
        {
            get { return taskDialogResult; }
            set { taskDialogResult = value; }
        }

        private string customButton;
        /// <summary>
        /// Gets or sets the text of the custom button that was clicked.
        /// </summary>
        public string CustomButton
        {
            get { return customButton; }
            set { customButton = value; }
        }

        
    }
}
