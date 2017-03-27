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
    using System.Text;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;
    using System.Collections.Generic;
    using System.Diagnostics;
    
    /// <summary>
    /// The UndoPrimitive for a text buffer change operation.
    /// </summary>
    internal class TextBufferChangeUndoPrimitive : TextUndoPrimitive
    {
        #region Private Data Members

        private bool _canUndo;

        private readonly ITextUndoHistory _undoHistory;
        private WeakReference _weakBufferReference;

        private readonly INormalizedTextChangeCollection _textChanges;
        private int? _beforeVersion;
        private int? _afterVersion;
#if DEBUG
        private int _bufferLengthAfterChange;
#endif

        #endregion // Private Data Members

        /// <summary>
        /// Constructs a TextBufferChangeUndoPrimitive.
        /// </summary>
        /// <param name="undoHistory">
        /// The ITextUndoHistory this change will be added to.
        /// </param>
        /// <param name="textVersion">
        /// The <see cref="ITextVersion" /> representing this change.
        /// This is actually the version associated with the snapshot prior to the change.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="undoHistory"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="textVersion"/> is null.</exception>
        public TextBufferChangeUndoPrimitive(ITextUndoHistory undoHistory, ITextVersion textVersion)
        {
            // Verify input parameters
            if (undoHistory == null)
            {
                throw new ArgumentNullException("undoHistory");
            }

            if (textVersion == null)
            {
                throw new ArgumentNullException("textVersion");
            }

            _textChanges = textVersion.Changes;
            _beforeVersion = textVersion.ReiteratedVersionNumber;
            _afterVersion = textVersion.Next.VersionNumber;
            Debug.Assert(textVersion.Next.VersionNumber == textVersion.Next.ReiteratedVersionNumber,
                "Creating a TextBufferChangeUndoPrimitive for a change that has previously been undone?  This is probably wrong.");

            _undoHistory = undoHistory;
            TextBuffer = textVersion.TextBuffer;
            AttachedToNewBuffer = false;

            _canUndo = true;

#if DEBUG
            // for debug sanity checks
            _bufferLengthAfterChange = textVersion.Next.Length;
#endif
        }

        #region UndoPrimitive Members

        /// <summary>
        /// Returns true if operation can be undone, false otherwise.
        /// </summary>
        public override bool CanUndo
        {
            get
            {
                // NOTE: We don't know for sure if we can undo (it might get blocked by a readonly region or a
                // canceled edit), in which case the actual Undo() will fail.
                return _canUndo;
            }
        }

        /// <summary>
        /// Returns true if operation can be redone, false otherwise.
        /// </summary>
        public override bool CanRedo
        {
            get
            {
                // NOTE: We don't know for sure if we can redo (it might get blocked by a readonly region or a
                // canceled edit), in which case the actual Do() will fail.
                return !_canUndo;
            }
        }

        /// <summary>
        /// Redo the text buffer change action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be redone.</exception>
        public override void Do()
        {
            // Validate, we shouldn't be allowed to undo
            if (!CanRedo)
            {
                throw new InvalidOperationException(Strings.CannotRedo);
            }

            // For undo-in-closed-files scenarios where we are done/undone on a buffer other
            // than the one we were originally created on.
            if (AttachedToNewBuffer)
            {
                AttachedToNewBuffer = false;

                _beforeVersion = TextBuffer.CurrentSnapshot.Version.VersionNumber;
                _afterVersion = null;
            }

            bool editCanceled = false;
            using (ITextEdit edit = TextBuffer.CreateEdit(EditOptions.None, _afterVersion, typeof(TextBufferChangeUndoPrimitive)))
            {
                foreach (ITextChange textChange in _textChanges)
                {
                    if (!edit.Replace(new Span(textChange.OldPosition, textChange.OldLength), textChange.NewText))
                    {
                        // redo canceled by readonly region
                        editCanceled = true;
                        break;
                    }
                }

                if (!editCanceled)
                {
                    edit.Apply();

                    if (edit.Canceled)
                    {
                        editCanceled = true;
                    }
                }
            }

            if (editCanceled)
            {
                throw new OperationCanceledException("Redo failed due to readonly regions or canceled edit.");
            }

            if (_afterVersion == null)
            {
                _afterVersion = TextBuffer.CurrentSnapshot.Version.VersionNumber;
            }

#if DEBUG
            // sanity check
            Debug.Assert(TextBuffer.CurrentSnapshot.Length == _bufferLengthAfterChange,
                         "The buffer is in a different state than when this TextBufferChangeUndoPrimitive was created!");
#endif

            _canUndo = true;
        }

        /// <summary>
        /// Undo the text buffer change action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Undo()
        {
            // Validate that we can undo this change
            if (!CanUndo)
            {
                throw new InvalidOperationException(Strings.CannotUndo);
            }

#if DEBUG
            // sanity check
            Debug.Assert(TextBuffer.CurrentSnapshot.Length == _bufferLengthAfterChange,
                         "The buffer is in a different state than when this TextBufferUndoChangePrimitive was created!");
#endif

            // For undo-in-closed-files scenarios where we are done/undone on a buffer other
            // than the one we were originally created on.
            if (AttachedToNewBuffer)
            {
                AttachedToNewBuffer = false;

                _beforeVersion = null;
                _afterVersion = TextBuffer.CurrentSnapshot.Version.VersionNumber;
            }

            bool editCanceled = false;
            using (ITextEdit edit = TextBuffer.CreateEdit(EditOptions.None, _beforeVersion, typeof(TextBufferChangeUndoPrimitive)))
            {
                foreach (ITextChange textChange in _textChanges)
                {
                    if (!edit.Replace(new Span(textChange.NewPosition, textChange.NewLength), textChange.OldText))
                    {
                        // undo canceled by readonly region
                        editCanceled = true;
                        break;
                    }
                }

                if (!editCanceled)
                {
                    edit.Apply();

                    if (edit.Canceled)
                    {
                        editCanceled = true;
                    }
                }
            }

            if (editCanceled)
            {
                throw new OperationCanceledException("Undo failed due to readonly regions or canceled edit.");
            }

            if (_beforeVersion == null)
            {
                _beforeVersion = TextBuffer.CurrentSnapshot.Version.VersionNumber;
            }

            _canUndo = false;
        }

        public override bool CanMerge(ITextUndoPrimitive older)
        {
            return false;
        }
        #endregion

        #region Private Helpers
        /// <summary>
        /// We track our ITextBuffer in ITextUndoHistory.Properties so that we can be redirected to act on a 
        /// different ITextBuffer in the undo-in-closed-files scenario.
        /// </summary>
        private ITextBuffer TextBuffer
        {
            get
            {
                ITextBuffer buffer;
                if (!_undoHistory.Properties.TryGetProperty(typeof(ITextBuffer), out buffer))
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException("ITextUndoHistory.Properties must contain an entry for the ITextBuffer this TextBufferChangeUndoPrimitive should act against.");
                }

                return buffer;
            }
            set
            {
                _undoHistory.Properties[typeof(ITextBuffer)] = value;
            }
        }

        private bool AttachedToNewBuffer
        {
            get
            {
                return _weakBufferReference.Target != TextBuffer;
            }
            set
            {
                if (value != false)
                {
                    throw new InvalidOperationException("AttachedToNewBuffer can only be reset to false.");
                }
                Debug.Assert(TextBuffer != null);
                _weakBufferReference = new WeakReference(TextBuffer);
            }
        }
        #endregion
    }
}
