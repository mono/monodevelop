//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Specifies the initial display location for a task dialog. 
    /// </summary>
    public enum TaskDialogStartupLocation
    {
        /// <summary>
        /// The window placed in the center of the screen.
        /// </summary>
        CenterScreen,

        /// <summary>
        /// The window centered relative to the window that launched the dialog.
        /// </summary>
        CenterOwner
    }
}
