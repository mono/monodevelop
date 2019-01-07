//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Ide.Text
{
    /// <summary>
    /// Provides a very simple ITextUndoHistory implementation.
    /// </summary>
    internal sealed class UndoHistory : ITextUndoHistory
    {
        private readonly Stack<UndoTransaction> _openTransactionStack = new Stack<UndoTransaction>();
        private readonly Stack<ITextUndoTransaction> _undoStack = new Stack<ITextUndoTransaction>();
        private readonly Stack<ITextUndoTransaction> _redoStack = new Stack<ITextUndoTransaction>();
        private readonly PropertyCollection _properties = new PropertyCollection();
        private TextUndoHistoryState _state = TextUndoHistoryState.Idle;
        private event EventHandler<TextUndoRedoEventArgs> _undoRedoHappened;
        private event EventHandler<TextUndoTransactionCompletedEventArgs> _undoTransactionCompleted;

        internal ITextUndoTransaction CurrentTransaction
        {
            get { return _openTransactionStack.Count > 0 ? _openTransactionStack.Peek() : null; }
        }

        internal Stack<ITextUndoTransaction> UndoStack => _undoStack;

        internal Stack<ITextUndoTransaction> RedoStack => _redoStack;

        internal object Context { get; }

        internal UndoHistory(object context) => Context = context;

        internal ITextUndoTransaction CreateTransaction(string description)
        {
            _openTransactionStack.Push(new UndoTransaction(this, description));
            return _openTransactionStack.Peek();
        }

        internal void Redo(int count)
        {
            try
            {
                count = Math.Min(_redoStack.Count, count);
                _state = TextUndoHistoryState.Redoing;
                for (var i = 0; i < count; i++)
                {
                    var current = _redoStack.Peek();
                    if (!current.CanRedo)
                    {
                        throw new InvalidOperationException();
                    }

                    _redoStack.Pop();
                    current.Do();
                    _undoStack.Push(current);
                }

                RaiseUndoRedoHappened();
            }
            finally
            {
                _state = TextUndoHistoryState.Idle;
            }
        }

        internal void Undo(int count)
        {
            try
            {
                count = Math.Min(_undoStack.Count, count);
                _state = TextUndoHistoryState.Undoing;
                for (var i = 0; i < count; i++)
                {
                    var current = _undoStack.Peek();
                    if (!current.CanUndo)
                    {
                        throw new InvalidOperationException();
                    }
                    _undoStack.Pop();
                    current.Undo();
                    _redoStack.Push(current);

                    RaiseUndoRedoHappened();
                }
            }
            finally
            {
                _state = TextUndoHistoryState.Idle;
            }
        }

        internal void OnTransactionClosed(UndoTransaction transaction, bool didComplete)
        {
            if (_openTransactionStack.Count == 0 || transaction != _openTransactionStack.Peek())
            {
                // Happens in dispose after complete / cancel
                return;
            }

            _openTransactionStack.Pop();
            if (!didComplete)
            {
                return;
            }

            if (_openTransactionStack.Count == 0)
            {
                OnTransactionStackCompleted(transaction);
            }
            else
            {
                var top = _openTransactionStack.Peek();
                foreach (var cur in transaction.UndoPrimitives)
                {
                    top.AddUndo(cur);
                }

                transaction.UndoPrimitives.Clear();
            }
        }

        private void OnTransactionStackCompleted(UndoTransaction transaction)
        {
            Debug.Assert(_openTransactionStack.Count == 0);

            ITextUndoTransaction eventArgument = transaction;
            var result = TextUndoTransactionCompletionResult.TransactionAdded;
            if (_undoStack.Count > 0)
            {
                var previous = _undoStack.Peek();
                if (transaction.MergePolicy != null &&
                    previous.MergePolicy != null &&
                    previous.MergePolicy.TestCompatiblePolicy(transaction.MergePolicy) &&
                    previous.MergePolicy.CanMerge(newerTransaction: transaction, olderTransaction: previous))
                {
                    eventArgument = previous;
                    previous.MergePolicy.PerformTransactionMerge(existingTransaction: previous, newTransaction: transaction);
                }
                else
                {
                    _undoStack.Push(transaction);
                }
            }
            else
            {
                _undoStack.Push(transaction);
            }

            _undoTransactionCompleted?.Invoke(this, new TextUndoTransactionCompletedEventArgs(eventArgument, result));
        }

        internal void Clear()
        {
            if (_state != TextUndoHistoryState.Idle || CurrentTransaction != null)
            {
                throw new InvalidOperationException("Can't clear with an open transaction or in undo / redo");
            }

            _undoStack.Clear();
            _redoStack.Clear();

            // The IEditorOperations AddAfterTextBufferChangePrimitive and AddBeforeTextBufferChangePrimitive
            // implementations store an ITextView in the Property of the associated ITextUndoHistory.  It's
            // necessary to keep this value present so long as the primitives are in the undo / redo stack
            // as their implementation depends on it.  Once the stack is cleared we can safely remove
            // the value.
            //
            // This is in fact necessary for sane testing.  Without this removal it's impossible to have
            // an ITextView disconnect and be collected from it's underlying ITextBuffer.  The ITextUndoHistory
            // is associated with an ITextBuffer and through it's undo stack will keep the ITextView alive
            // indefinitely
            _properties.RemoveProperty(typeof(ITextView));
        }

        private void RaiseUndoRedoHappened()
        {
            // Note: Passing null here as this is what Visual Studio does
            _undoRedoHappened?.Invoke(this, new TextUndoRedoEventArgs(_state, null));
        }

        bool ITextUndoHistory.CanRedo => _redoStack.Count > 0;

        bool ITextUndoHistory.CanUndo => _undoStack.Count > 0;

        ITextUndoTransaction ITextUndoHistory.CreateTransaction(string description) => CreateTransaction(description);

        ITextUndoTransaction ITextUndoHistory.CurrentTransaction => CurrentTransaction;

        /// <summary>
        /// Easy to implement but not supported by Visual Studio
        /// </summary>
        ITextUndoTransaction ITextUndoHistory.LastRedoTransaction => throw new NotSupportedException();

        /// <summary>
        /// Easy to implement but not supported by Visual Studio
        /// </summary>
        ITextUndoTransaction ITextUndoHistory.LastUndoTransaction => throw new NotSupportedException();

        void ITextUndoHistory.Redo(int count) => Redo(count);

        /// <summary>
        /// Easy to implement but not supported by Visual Studio
        /// </summary>
        string ITextUndoHistory.RedoDescription => throw new NotSupportedException();

        /// <summary>
        /// Easy to implement but not supported by Visual Studio
        /// </summary>
        IEnumerable<ITextUndoTransaction> ITextUndoHistory.RedoStack => _redoStack;

        TextUndoHistoryState ITextUndoHistory.State
        {
            [DebuggerNonUserCode]
            get => _state;
        }

        void ITextUndoHistory.Undo(int count) => Undo(count);

        /// <summary>
        /// Easy to implement but not supported by Visual Studio
        /// </summary>
        string ITextUndoHistory.UndoDescription => throw new NotSupportedException();

        event EventHandler<TextUndoRedoEventArgs> ITextUndoHistory.UndoRedoHappened
        {
            add => _undoRedoHappened += value;
            remove => _undoRedoHappened -= value;
        }

        /// <summary>
        /// Easy to implement but not supported by Visual Studio
        /// </summary>
        IEnumerable<ITextUndoTransaction> ITextUndoHistory.UndoStack => _undoStack;

        event EventHandler<TextUndoTransactionCompletedEventArgs> ITextUndoHistory.UndoTransactionCompleted
        {
            add => _undoTransactionCompleted += value;
            remove => _undoTransactionCompleted -= value;
        }

        PropertyCollection IPropertyOwner.Properties => _properties;
    }
}
