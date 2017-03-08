//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// The UndoPrimitive to take place on the Undo stack after a text buffer change. This is the simpler
    /// version of the primitive that handles most common cases.
    /// </summary>
    internal class AfterTextBufferChangeUndoPrimitive : TextUndoPrimitive
    {
        // Think twice before adding any fields here! These objects are long-lived and consume considerable space.
        // Unusual cases should be handled by the GeneralAfterTextBufferChangedUndoPrimitive class below.
        protected ITextUndoHistory _undoHistory;
        protected int _newCaretIndex;
        protected byte _newCaretAffinityByte;
        protected bool _canUndo;

        /// <summary>
        /// Constructs a AfterTextBufferChangeUndoPrimitive.
        /// </summary>
        /// <param name="textView">
        /// The text view that was responsible for causing this change.
        /// </param>
        /// <param name="undoHistory">
        /// The <see cref="ITextUndoHistory" /> this primitive will be added to.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="textView"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="undoHistory"/> is null.</exception>
        public static AfterTextBufferChangeUndoPrimitive Create(ITextView textView, ITextUndoHistory undoHistory)
        {
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }
            if (undoHistory == null)
            {
                throw new ArgumentNullException("undoHistory");
            }

            // Store the ITextView for these changes in the ITextUndoHistory properties so we can retrieve it later.
            if (!undoHistory.Properties.ContainsProperty(typeof(ITextView)))
            {
                undoHistory.Properties[typeof(ITextView)] = textView;
            }

            IMapEditToData map = BeforeTextBufferChangeUndoPrimitive.GetMap(textView);

            CaretPosition caret = textView.Caret.Position;
            int newCaretIndex = BeforeTextBufferChangeUndoPrimitive.MapToData(map, caret.BufferPosition);
            int newCaretVirtualSpaces = caret.VirtualBufferPosition.VirtualSpaces;

            VirtualSnapshotPoint anchor = textView.Selection.AnchorPoint;
            int newSelectionAnchorIndex = BeforeTextBufferChangeUndoPrimitive.MapToData(map, anchor.Position);
            int newSelectionAnchorVirtualSpaces = anchor.VirtualSpaces;

            VirtualSnapshotPoint active = textView.Selection.ActivePoint;
            int newSelectionActiveIndex = BeforeTextBufferChangeUndoPrimitive.MapToData(map, active.Position);
            int newSelectionActiveVirtualSpaces = active.VirtualSpaces;

            TextSelectionMode newSelectionMode = textView.Selection.Mode;

            if (newCaretVirtualSpaces != 0 ||
                newSelectionAnchorIndex != newCaretIndex ||
                newSelectionAnchorVirtualSpaces != 0 ||
                newSelectionActiveIndex != newCaretIndex ||
                newSelectionActiveVirtualSpaces != 0 ||
                newSelectionMode != TextSelectionMode.Stream)
            {
                return new GeneralAfterTextBufferChangeUndoPrimitive
                            (undoHistory, newCaretIndex, caret.Affinity, newCaretVirtualSpaces, newSelectionAnchorIndex, 
                             newSelectionAnchorVirtualSpaces, newSelectionActiveIndex, newSelectionActiveVirtualSpaces, newSelectionMode);
            }
            else
            {
                return new AfterTextBufferChangeUndoPrimitive(undoHistory, newCaretIndex, caret.Affinity);
            }
        }

        protected AfterTextBufferChangeUndoPrimitive(ITextUndoHistory undoHistory, int caretIndex, PositionAffinity caretAffinity)
        {
            _undoHistory = undoHistory;
            _newCaretIndex = caretIndex;
            _newCaretAffinityByte = (byte)caretAffinity;
            _canUndo = true;
        }

        // Internal empty constructor for unit testing.
        internal AfterTextBufferChangeUndoPrimitive() { }

        /// <summary>
        /// The <see cref="ITextView"/> that this <see cref="AfterTextBufferChangeUndoPrimitive"/> is bound to.
        /// </summary>
        internal ITextView GetTextView()
        {
                ITextView view = null;
                _undoHistory.Properties.TryGetProperty(typeof(ITextView), out view);
                return view;
        }

        internal int CaretIndex
        {
            get { return _newCaretIndex; }
        }

        internal virtual int CaretVirtualSpace
        {
            get { return 0; }
        }

        #region ITextUndoPrimitive Members

        /// <summary>
        /// Returns true if operation can be undone, false otherwise.
        /// </summary>
        public override bool CanUndo
        {
            get { return _canUndo; }
        }

        /// <summary>
        /// Returns true if operation can be redone, false otherwise.
        /// </summary>
        public override bool CanRedo
        {
            get { return !_canUndo; }
        }

        /// <summary>
        /// Do the action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be redone.</exception>
        public override void Do()
        {
            // Validate, we shouldn't be allowed to undo
            if (!CanRedo)
            {
                throw new InvalidOperationException(Strings.CannotRedo);
            }

            // Set the new caret position and active selection
            var view = this.GetTextView();
            Debug.Assert(view == null || !view.IsClosed, "Attempt to undo/redo on a closed view?  This shouldn't happen.");
            if (view != null && !view.IsClosed)
            {
                DoMoveCaretAndSelect(view, BeforeTextBufferChangeUndoPrimitive.GetMap(view));
                view.Caret.EnsureVisible();
            }

            _canUndo = true;
        }

        /// <summary>
        /// Move the caret and restore the selection as part of the Redo operation.
        /// </summary>
        protected virtual void DoMoveCaretAndSelect(ITextView view, IMapEditToData map)
        {
            SnapshotPoint newCaret = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _newCaretIndex));

            view.Caret.MoveTo(newCaret, (PositionAffinity)_newCaretAffinityByte);
            view.Selection.Clear();
        }

        /// <summary>
        /// Undo the action.
        /// </summary>
        /// <exception cref="InvalidOperationException">Operation cannot be undone.</exception>
        public override void Undo()
        {
            // Validate that we can undo this change
            if (!CanUndo)
            {
                throw new InvalidOperationException(Strings.CannotUndo);
            }

            // Currently, no action is done on the Undo.  To restore the caret and selection after a text buffer undo, there is the BeforeTextBufferChangeUndoPrimitive.
            // This undo should not do anything with the caret and selection, because we only want to reset them after the TextBuffer change has occurred.
            // Therefore, we need to add this UndoPrimitive to the undo stack before the UndoPrimitive for the TextBuffer change.  On an redo, the TextBuffer changed UndoPrimitive
            // will fire it's Redo first, and than the Redo for this UndoPrimitive will fire.
            // However, on an undo, the undo for this UndoPrimitive will be fired, and then the undo for the TextBuffer change UndoPrimitive.  If we had set any caret placement/selection here (ie the old caret placement/selection), 
            // we may crash because the TextBuffer change has not occurred yet (ie you try to set the caret to be at CharacterIndex 1 when the TextBuffer is still empty).

            _canUndo = false;
        }

        public override bool CanMerge(ITextUndoPrimitive older)
        {
            return false;
        }
        #endregion
    }

    /// <summary>
    /// The UndoPrimitive to take place on the Undo stack before a text buffer change. This is the general
    /// version of the primitive that handles all cases, including those involving selections and virtual space.
    /// </summary>
    internal class GeneralAfterTextBufferChangeUndoPrimitive : AfterTextBufferChangeUndoPrimitive
    {
        private int _newCaretVirtualSpaces;
        private int _newSelectionAnchorIndex;
        private int _newSelectionAnchorVirtualSpaces;
        private int _newSelectionActiveIndex;
        private int _newSelectionActiveVirtualSpaces;
        private TextSelectionMode _newSelectionMode;

        public GeneralAfterTextBufferChangeUndoPrimitive(ITextUndoHistory undoHistory,
                                                         int newCaretIndex,
                                                         PositionAffinity newCaretAffinity,
                                                         int newCaretVirtualSpaces,
                                                         int newSelectionAnchorIndex,
                                                         int newSelectionAnchorVirtualSpaces,
                                                         int newSelectionActiveIndex,
                                                         int newSelectionActiveVirtualSpaces,
                                                         TextSelectionMode newSelectionMode)
            : base(undoHistory, newCaretIndex, newCaretAffinity)
        {
            _newCaretVirtualSpaces = newCaretVirtualSpaces;
            _newSelectionAnchorIndex = newSelectionAnchorIndex;
            _newSelectionAnchorVirtualSpaces = newSelectionAnchorVirtualSpaces;
            _newSelectionActiveIndex = newSelectionActiveIndex;
            _newSelectionActiveVirtualSpaces = newSelectionActiveVirtualSpaces;
            _newSelectionMode = newSelectionMode;
        }

        internal override int CaretVirtualSpace
        {
            get { return _newCaretVirtualSpaces; }
        }

        /// <summary>
        /// Move the caret and restore the selection as part of the Redo operation.
        /// </summary>
        protected override void DoMoveCaretAndSelect(ITextView view, IMapEditToData map)
        {
            SnapshotPoint newCaret = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _newCaretIndex));
            SnapshotPoint newAnchor = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _newSelectionAnchorIndex));
            SnapshotPoint newActive = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _newSelectionActiveIndex));

            view.Caret.MoveTo(new VirtualSnapshotPoint(newCaret, _newCaretVirtualSpaces), (PositionAffinity)_newCaretAffinityByte);

            view.Selection.Mode = _newSelectionMode;

            var virtualAnchor = new VirtualSnapshotPoint(newAnchor, _newSelectionAnchorVirtualSpaces);
            var virtualActive = new VirtualSnapshotPoint(newActive, _newSelectionActiveVirtualSpaces);

            // Buffer may have been changed by one of the listeners on the caret move event.
            virtualAnchor = virtualAnchor.TranslateTo(view.TextSnapshot);
            virtualActive = virtualActive.TranslateTo(view.TextSnapshot);

            view.Selection.Select(virtualAnchor, virtualActive);
        }
    }
}
