//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Identifies one of the standard buttons that 
    /// can be displayed via TaskDialog.
    /// </summary>
    [Flags]
    public enum TaskDialogStandardButtons
    {
        /// <summary>
        /// No buttons on the dialog.
        /// </summary>
        None    = 0x0000,   

        /// <summary>
        /// An "OK" button.
        /// </summary>
        Ok      = 0x0001,

        /// <summary>
        /// A "Yes" button.
        /// </summary>
        Yes     = 0x0002,

        /// <summary>
        /// A "No" button.
        /// </summary>
        No      = 0x0004,

        /// <summary>
        /// A "Cancel" button.
        /// </summary>
        Cancel  = 0x0008,

        /// <summary>
        /// A "Retry" button.
        /// </summary>
        Retry   = 0x0010,

        /// <summary>
        /// A "Close" button.
        /// </summary>
        Close   = 0x0020
    }
}
