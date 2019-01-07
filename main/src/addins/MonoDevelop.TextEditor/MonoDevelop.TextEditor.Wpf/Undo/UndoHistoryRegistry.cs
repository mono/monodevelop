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
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text.Operations;

namespace MonoDevelop.Ide.Text
{
    /// <summary>
    /// This class is intended to be a very simple ITextUndoHistoryRegistry implementation for hosts that
    /// don't have a built-in undo mechanism
    /// </summary>
    [Export(typeof(ITextUndoHistoryRegistry))]
    internal sealed class TextUndoHistoryRegistry : ITextUndoHistoryRegistry
    {
        private readonly ConditionalWeakTable<object, ITextUndoHistory> _map = new ConditionalWeakTable<object, ITextUndoHistory>();

        internal TextUndoHistoryRegistry()
        {
        }

        private bool TryGetHistory(object context, out ITextUndoHistory undoHistory)
        {
            return _map.TryGetValue(context, out undoHistory);
        }

        #region ITextUndoHistoryRegistry

        /// <summary>
        /// Easy to implement but the Visual Studio implementation throws a NotSupportedException
        /// </summary>
        void ITextUndoHistoryRegistry.AttachHistory(object context, ITextUndoHistory history)
        {
            throw new NotSupportedException();
        }

        ITextUndoHistory ITextUndoHistoryRegistry.GetHistory(object context)
        {
            ITextUndoHistory history;
            _map.TryGetValue(context, out history);
            return history;
        }

        ITextUndoHistory ITextUndoHistoryRegistry.RegisterHistory(object context)
        {
            ITextUndoHistory history;
            if (!_map.TryGetValue(context, out history))
            {
                history = new UndoHistory(context);
                _map.Add(context, history);
            }
            return history;
        }

        void ITextUndoHistoryRegistry.RemoveHistory(ITextUndoHistory history)
        {
            var undoHistory = history as UndoHistory;
            if (undoHistory != null)
            {
                _map.Remove(undoHistory.Context);
                undoHistory.Clear();
            }
        }

        bool ITextUndoHistoryRegistry.TryGetHistory(object context, out ITextUndoHistory history)
        {
            ITextUndoHistory undoHistory;
            if (TryGetHistory(context, out undoHistory))
            {
                history = undoHistory;
                return true;
            }

            history = null;
            return false;
        }

        #endregion
    }
}