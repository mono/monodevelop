//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    /// <summary>
    /// Enum value stating type of text edit action.
    /// </summary>
    internal enum TextEditAction
    {
        None,
        Type,
        Delete,
        Backspace,
        Paste,
        Enter,
        AutoIndent,
        Replace,
        ProvisionalOverwrite
    }
}
