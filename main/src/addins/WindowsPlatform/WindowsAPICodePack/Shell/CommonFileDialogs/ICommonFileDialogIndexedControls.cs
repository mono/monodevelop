//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Dialogs.Controls
{
    /// <summary>
    /// Specifies a property, event and method that indexed controls need
    /// to implement.
    /// </summary>
    /// 
    /// <remarks>
    /// not sure where else to put this, so leaving here for now.
    /// </remarks>
    interface ICommonFileDialogIndexedControls
    {
        int SelectedIndex { get; set; }

        event EventHandler SelectedIndexChanged;

        void RaiseSelectedIndexChangedEvent();
    }
}