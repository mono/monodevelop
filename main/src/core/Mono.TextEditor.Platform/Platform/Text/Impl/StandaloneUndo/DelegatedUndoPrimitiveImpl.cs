//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    /// <summary>
    /// This is the implementation of a primitive to support inverse operations, where the user does not supply their own
    /// primitives. Rather, the user calls "AddUndo" on the history and we build the primitive for them.
    /// </summary>
    internal class DelegatedUndoPrimitiveImpl : ITextUndoPrimitive
    {
        private Stack<UndoableOperationCurried> redoOperations;
        private Stack<UndoableOperationCurried> undoOperations;
        private UndoTransactionImpl parent;
        private readonly UndoHistoryImpl history;
        private DelegatedUndoPrimitiveState state;

        public DelegatedUndoPrimitiveState State
        {
            get { return state; }
            set { state = value; }
        }

        public DelegatedUndoPrimitiveImpl(UndoHistoryImpl history, UndoTransactionImpl parent, UndoableOperationCurried operationCurried)
        {
            redoOperations = new Stack<UndoableOperationCurried>();
            undoOperations = new Stack<UndoableOperationCurried>();

            this.parent = parent;
            this.history = history;
            this.state = DelegatedUndoPrimitiveState.Inactive;

            undoOperations.Push(operationCurried);
        }

        public bool CanRedo
        {
            get { return redoOperations.Count > 0; }
        }

        public bool CanUndo
        {
            get { return undoOperations.Count > 0; }
        }


        /// <summary>
        /// Here, we undo everything in the list of undo operations, and then clear the list. While this is happening, the
        /// History will collect new operations for the redo list and pass them on to us.
        /// </summary>
        public void Undo()
        {
            using (new CatchOperationsFromHistoryForDelegatedPrimitive(history, this, DelegatedUndoPrimitiveState.Undoing))
            {
                while (undoOperations.Count > 0)
                {
                    undoOperations.Pop()();
                }
            }
        }

        /// <summary>
        /// This is only called for "Redo," not for the original "Do." The action is to redo everything in the list of
        /// redo operations, and then clear the list. While this is happening, the History will collect new operations
        /// for the undo list and pass them on to us.
        /// </summary>
        public void Do()
        {
            using (new CatchOperationsFromHistoryForDelegatedPrimitive(history, this, DelegatedUndoPrimitiveState.Redoing))
            {
                while (redoOperations.Count > 0)
                {
                    redoOperations.Pop()();
                }
            }
        }

        public ITextUndoTransaction Parent
        {
            get { return this.parent; }
            set { this.parent = value as UndoTransactionImpl; }
        }

        /// <summary>
        /// This is called by the UndoHistory implementation when we are mid-undo/mid-redo and
        /// the history receives a new UndoableOperation. The action is then to add that operation
        /// to the inverse list.
        /// </summary>
        /// <param name="operation"></param>
        public void AddOperation(UndoableOperationCurried operation)
        {
            if (this.state == DelegatedUndoPrimitiveState.Redoing)
            {
                undoOperations.Push(operation);
            }
            else if (this.state == DelegatedUndoPrimitiveState.Undoing)
            {
                redoOperations.Push(operation);
            }
            else
            {
                throw new InvalidOperationException("Strings.DelegatedUndoPrimitiveStateDoesNotAllowAdd");
            }
        }

        public bool MergeWithPreviousOnly
        {
            get { return true; }
        }

        public bool CanMerge(ITextUndoPrimitive primitive)
        {
            return false;
        }

        public ITextUndoPrimitive Merge(ITextUndoPrimitive primitive)
        {
            throw new InvalidOperationException("Strings.DelegatedUndoPrimitiveCannotMerge");
        }
    }
}