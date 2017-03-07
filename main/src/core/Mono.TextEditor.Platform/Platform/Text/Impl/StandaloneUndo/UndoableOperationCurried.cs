//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    /// <summary>
    /// This is the delegate that we ultimately call to perform the work of the
    /// delegated undo operations. It contains information about all the parameter
    /// objects as well as the history of origin.
    /// </summary>
    internal delegate void UndoableOperationCurried();
}
