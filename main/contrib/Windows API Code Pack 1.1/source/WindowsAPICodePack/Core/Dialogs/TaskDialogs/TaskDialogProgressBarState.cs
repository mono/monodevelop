//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Sets the state of a task dialog progress bar.
    /// </summary>        
    public enum TaskDialogProgressBarState
    {
        /// <summary>
        /// Uninitialized state, this should never occur.
        /// </summary>
        None = 0,

        /// <summary>
        /// Normal state.
        /// </summary>
        Normal = TaskDialogNativeMethods.ProgressBarState.Normal,

        /// <summary>
        /// An error occurred.
        /// </summary>
        Error = TaskDialogNativeMethods.ProgressBarState.Error,

        /// <summary>
        /// The progress is paused.
        /// </summary>
        Paused = TaskDialogNativeMethods.ProgressBarState.Paused,

        /// <summary>
        /// Displays marquee (indeterminate) style progress
        /// </summary>
        Marquee
    }
}
