//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BufferUndoManager.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;
    using System.Diagnostics;

    class TextBufferUndoManager : ITextBufferUndoManager, IDisposable
    {
        #region Private Members

        ITextBuffer _textBuffer;
        ITextUndoHistoryRegistry _undoHistoryRegistry;
        ITextUndoHistory _undoHistory;
        Queue<ITextVersion> _editVersionList = new Queue<ITextVersion>();
        bool _inPostChanged;

        #endregion

        public TextBufferUndoManager(ITextBuffer textBuffer, ITextUndoHistoryRegistry undoHistoryRegistry)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            if (undoHistoryRegistry == null)
            {
                throw new ArgumentNullException("undoHistoryRegistry");
            }

            _textBuffer = textBuffer;

            _undoHistoryRegistry = undoHistoryRegistry;

            // Register the undo history
            _undoHistory = _undoHistoryRegistry.RegisterHistory(_textBuffer);

            // Listen for the buffer changed events so that we can make them undo/redo-able
            _textBuffer.Changed += TextBufferChanged;
            _textBuffer.PostChanged += TextBufferPostChanged;
            _textBuffer.Changing += TextBufferChanging;
        }

        #region Private Methods

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            Debug.Assert((e.EditTag as Type) != typeof(TextBufferChangeUndoPrimitive) ||
                         (_undoHistory.State != TextUndoHistoryState.Idle),
                         "We are undoing/redoing a change while UndoHistory.State is Idle.  Something is wrong with the state.");

            // If this change didn't originate from undo, add a TextBufferChangeUndoPrimitive to our history.
            if (_undoHistory.State == TextUndoHistoryState.Idle &&
                (e.EditTag as Type) != typeof(TextBufferChangeUndoPrimitive))
            {
                // With projection, we sometimes get Changed events with no changes, or for "" -> "".
                // We don't want to create undo actions for these.
                bool nonNullChange = false;
                foreach (ITextChange c in e.BeforeVersion.Changes)
                {
                    if (c.OldLength != 0 || c.NewLength != 0)
                    {
                        nonNullChange = true;
                        break;
                    }
                }

                if (nonNullChange)
                {
                    // Queue the edit, and actually add an undo primitive later (see comment on PostChanged).
                    _editVersionList.Enqueue(e.BeforeVersion);
                }
            }
        }

        /// <remarks>
        /// Edits are queued up by our TextBufferChanged handler and then we finally add them to the
        /// undo stack here in response to PostChanged.  The reason and history behind why we do this 
        /// is as follows:
        ///
        /// Originally this was done for VB commit, which uses undo events (i.e. TransactionCompleted) to 
        /// trigger commit.  Their commit logic relies on the buffer being in a state such that applying 
        /// an edit synchronously raises a Changed event (which is always the case for PostChanged, but 
        /// not for Changed if there are nested edits).
        ///
        /// JaredPar made a change (CS 1182244) that allowed VB to detect that UndoTransactionCompleted 
        /// was being fired from a nested edit, and therefore delay the actual commit until the following 
        /// PostChanged event.
        ///
        /// So this allowed us to move TextBufferUndoManager back to adding undo actions directly 
        /// from the TextBufferChanged handler (CS 1285117).  This is preferable, as otherwise there's a 
        /// "delay" between when the edit happens and when we record the edit on the undo stack, 
        /// allowing other people to stick something on the undo stack (i.e. from 
        /// their ITextBuffer.Changed handler) in between.  The result is actions being "out-of-order" 
        /// on the undo stack.
        ///
        /// Unfortunately, it turns out VB snippets actually rely on this "out-of-order" behavior
        /// (see Dev10 834740) and so we are forced to revert CS 1285117) and return to the model
        /// where we queue up edits and delay adding them to the undo stack until PostChanged.
        ///
        /// It would be good to revisit this at again, but we would need to work with VB
        /// to fix their snippets / undo behavior, and verify that VB commit is also unaffected.
        /// </remarks>
        private void TextBufferPostChanged(object sender, EventArgs e)
        {
            // Only process a top level PostChanged event.  Nested events will continue to process TextChange events
            // which are added to the queue and will be processed below
            if ( _inPostChanged )
            {
                return;
            }

            _inPostChanged = true;
            try
            {
                // Do not do a foreach loop here.  It's perfectly possible, and in fact expected, that the Complete
                // method below can trigger a series of events which leads to a nested edit and another 
                // ITextBuffer::Changed.  That event will add to the _editVersionList queue and hence break a 
                // foreach loop
                while ( _editVersionList.Count > 0 )
                {
                    var cur = _editVersionList.Dequeue();
                    using (ITextUndoTransaction undoTransaction = _undoHistory.CreateTransaction(Strings.TextBufferChanged))
                    {
                        TextBufferChangeUndoPrimitive undoPrimitive = new TextBufferChangeUndoPrimitive(_undoHistory, cur);
                        undoTransaction.AddUndo(undoPrimitive);

                        undoTransaction.Complete();
                    }
                }
            }
            finally
            {
                _editVersionList.Clear();   // Ensure we cleanup state in the face of an exception
                _inPostChanged = false;
            }
        }

        void TextBufferChanging(object sender, TextContentChangingEventArgs e)
        {
            // See if somebody (other than us) is trying to edit the buffer during undo/redo.
            if (_undoHistory.State != TextUndoHistoryState.Idle &&
                (e.EditTag as Type) != typeof(TextBufferChangeUndoPrimitive))
            {
                Debug.Fail("Attempt to edit the buffer during undo/redo has been denied.  This is explicitly prohibited as it would corrupt the undo stack!  Please fix your code.");
                e.Cancel();
            }
        }


        #endregion

        #region ITextBufferUndoManager Members

        public ITextBuffer TextBuffer
        {
            get { return _textBuffer; }
        }

        public ITextUndoHistory TextBufferUndoHistory
        {
            // Note, right now, there is no way for us to know if an ITextUndoHistory
            // has been unregistered (ie it can be unregistered by a third party)
            // An issue has been logged with the Undo team, but in the mean time, to ensure that
            // we are robust, always register the undo history.
            get
            {
                _undoHistory = _undoHistoryRegistry.RegisterHistory(_textBuffer);
                return _undoHistory; 
            }
        }

        public void UnregisterUndoHistory()
        {
            // Unregister the undo history
            _undoHistoryRegistry.RemoveHistory(_undoHistory);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _textBuffer.Changed -= TextBufferChanged;
            _textBuffer.PostChanged -= TextBufferPostChanged;
            _textBuffer.Changing -= TextBufferChanging;

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
