//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Specifies the icon displayed in a task dialog.
    /// </summary>
    public enum TaskDialogStandardIcon
    {
        /// <summary>
        /// Displays no icons (default).
        /// </summary>
        None = 0,

        /// <summary>
        /// Displays the warning icon.
        /// </summary>
        Warning = 65535,

        /// <summary>
        /// Displays the error icon.
        /// </summary>
        Error = 65534,

        /// <summary>
        /// Displays the Information icon.
        /// </summary>
        Information = 65533,

        /// <summary>
        /// Displays the User Account Control shield.
        /// </summary>
        Shield = 65532 
    }
}
