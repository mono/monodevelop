// Copyright (c) Microsoft Corporation
// All rights reserved

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
