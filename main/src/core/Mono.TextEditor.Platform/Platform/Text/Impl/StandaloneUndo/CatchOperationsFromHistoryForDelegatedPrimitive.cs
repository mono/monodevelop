//********************************************************************************
// Copyright (c) Microsoft Corporation Inc. All rights reserved
//********************************************************************************

using System;

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    /// <summary>
    /// This class is to make it easy to catch new undo/redo operations while a delegated primitive
    /// is in progress--it is called from DelegatedUndoPrimitive.Undo and .Redo with the IDispose
    /// using pattern to set up the history to send operations our way.
    /// </summary>
    internal class CatchOperationsFromHistoryForDelegatedPrimitive : IDisposable
    {
        UndoHistoryImpl history;
        DelegatedUndoPrimitiveImpl primitive;

        public CatchOperationsFromHistoryForDelegatedPrimitive(UndoHistoryImpl history, DelegatedUndoPrimitiveImpl primitive, DelegatedUndoPrimitiveState state)
        {
            this.history = history;
            this.primitive = primitive;

            primitive.State = state;
            history.ForwardToUndoOperation(primitive);
        }

        public void Dispose()
        {
            history.EndForwardToUndoOperation(primitive);
            primitive.State = DelegatedUndoPrimitiveState.Inactive;
            GC.SuppressFinalize(this);
        }
    }
}
