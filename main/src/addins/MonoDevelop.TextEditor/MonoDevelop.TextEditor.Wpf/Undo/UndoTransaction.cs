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
using System.Linq;
using Microsoft.VisualStudio.Text.Operations;

namespace MonoDevelop.Ide.Text
{
    internal sealed class UndoTransaction : ITextUndoTransaction
    {
        private readonly UndoHistory _textUndoHistory;
        private readonly List<ITextUndoPrimitive> _primitiveList = new List<ITextUndoPrimitive>();
        private bool _completed;

        internal string Description
        {
            get;
            set;
        }

        internal List<ITextUndoPrimitive> UndoPrimitives
        {
            get { return _primitiveList; }
        }

        internal bool CanRedo
        {
            get { return _primitiveList.All(x => x.CanRedo); }
        }

        internal bool CanUndo
        {
            get { return _primitiveList.All(x => x.CanUndo); }
        }

        internal IMergeTextUndoTransactionPolicy MergePolicy
        {
            get;
            set;
        }

        internal UndoTransaction(UndoHistory textUndoHistory, string description)
        {
            _textUndoHistory = textUndoHistory;
            Description = description;
        }

        internal void AddUndo(ITextUndoPrimitive undo)
        {
            System.Diagnostics.Debug.Assert(undo.CanUndo);
            _primitiveList.Add(undo);
            undo.Parent = this;
        }

        private void HandleCompleted()
        {
            if (_completed)
            {
                throw new InvalidOperationException();
            }

            _completed = true;
        }

        #region ITextUndoTransaction

        void ITextUndoTransaction.AddUndo(ITextUndoPrimitive undo)
        {
            AddUndo(undo);
        }

        /// <summary>
        /// Visual Studio implementation throw so duplicate here
        /// </summary>
        bool ITextUndoTransaction.CanRedo
        {
            get { return CanRedo; }
        }

        /// <summary>
        /// Visual Studio implementation throw so duplicate here
        /// </summary>
        bool ITextUndoTransaction.CanUndo
        {
            get { return CanUndo; }
        }

        ITextUndoHistory ITextUndoTransaction.History
        {
            get { return _textUndoHistory; }
        }

        IMergeTextUndoTransactionPolicy ITextUndoTransaction.MergePolicy
        {
            get { return MergePolicy; }
            set { MergePolicy = value; }
        }

        ITextUndoTransaction ITextUndoTransaction.Parent
        {
            get { throw new NotSupportedException(); }
        }

        IList<ITextUndoPrimitive> ITextUndoTransaction.UndoPrimitives
        {
            get { return UndoPrimitives; }
        }

        UndoTransactionState ITextUndoTransaction.State
        {
            get { throw new NotSupportedException(); }
        }

        string ITextUndoTransaction.Description
        {
            get { return Description; }
            set { Description = value; }
        }

        void ITextUndoTransaction.Cancel()
        {
            HandleCompleted();
            _textUndoHistory.OnTransactionClosed(this, didComplete: false);
        }

        void ITextUndoTransaction.Complete()
        {
            HandleCompleted();
            _textUndoHistory.OnTransactionClosed(this, didComplete: true);
        }

        void ITextUndoTransaction.Do()
        {
            for (var i = 0; i < _primitiveList.Count; i++)
            {
                _primitiveList[i].Do();
            }
        }

        void ITextUndoTransaction.Undo()
        {
            for (var i = _primitiveList.Count - 1; i >= 0; i--)
            {
                _primitiveList[i].Undo();
            }
        }

        #endregion

        #region IDisposable

        void IDisposable.Dispose()
        {
            _textUndoHistory.OnTransactionClosed(this, didComplete: false);
        }

        #endregion
    }
}
