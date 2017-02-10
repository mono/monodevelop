//********************************************************************************
// Copyright (c) Microsoft Corporation Inc. All rights reserved
//********************************************************************************

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    /// <summary>
    /// This is the delegate that we ultimately call to perform the work of the
    /// delegated undo operations. It contains information about all the parameter
    /// objects as well as the history of origin.
    /// </summary>
    internal delegate void UndoableOperationCurried();
}
