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
    /// The UndoPrimitive to take place on the Undo stack before a text buffer change. This is the simpler
    /// version of the primitive that handles most common cases.
    /// </summary>
    internal class BeforeTextBufferChangeUndoPrimitive : TextUndoPrimitive
    {
        // Think twice before adding any fields here! These objects are long-lived and consume considerable space.
        // Unusual cases should be handled by the GeneralAfterTextBufferChangedUndoPrimitive class below.
        protected ITextUndoHistory _undoHistory;
        protected int _oldCaretIndex;
        protected byte _oldCaretAffinityByte;
        protected bool _canUndo;

        /// <summary>
        /// Constructs a BeforeTextBufferChangeUndoPrimitive.
        /// </summary>
        /// <param name="textView">
        /// The text view that was responsible for causing this change.
        /// </param>
        /// <param name="undoHistory">
        /// The <see cref="ITextUndoHistory" /> this primitive will be added to.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="textView"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="undoHistory"/> is null.</exception>
        public static BeforeTextBufferChangeUndoPrimitive Create(ITextView textView, ITextUndoHistory undoHistory)
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

            CaretPosition caret = textView.Caret.Position;

            IMapEditToData map = BeforeTextBufferChangeUndoPrimitive.GetMap(textView);

            int oldCaretIndex = BeforeTextBufferChangeUndoPrimitive.MapToData(map, caret.BufferPosition);
            int oldCaretVirtualSpaces = caret.VirtualBufferPosition.VirtualSpaces;

            VirtualSnapshotPoint anchor = textView.Selection.AnchorPoint;
            int oldSelectionAnchorIndex = BeforeTextBufferChangeUndoPrimitive.MapToData(map, anchor.Position);
            int oldSelectionAnchorVirtualSpaces = anchor.VirtualSpaces;

            VirtualSnapshotPoint active = textView.Selection.ActivePoint;
            int oldSelectionActiveIndex = BeforeTextBufferChangeUndoPrimitive.MapToData(map, active.Position);
            int oldSelectionActiveVirtualSpaces = active.VirtualSpaces;

            TextSelectionMode oldSelectionMode = textView.Selection.Mode;

            if (oldCaretVirtualSpaces != 0 ||
                oldSelectionAnchorIndex != oldCaretIndex ||
                oldSelectionAnchorVirtualSpaces != 0 ||
                oldSelectionActiveIndex != oldCaretIndex ||
                oldSelectionActiveVirtualSpaces != 0 ||
                oldSelectionMode != TextSelectionMode.Stream)
            {
                return new GeneralBeforeTextBufferChangeUndoPrimitive
                            (undoHistory, oldCaretIndex, caret.Affinity, oldCaretVirtualSpaces, oldSelectionAnchorIndex,
                             oldSelectionAnchorVirtualSpaces, oldSelectionActiveIndex, oldSelectionActiveVirtualSpaces, oldSelectionMode);
            }
            else
            {
                return new BeforeTextBufferChangeUndoPrimitive(undoHistory, oldCaretIndex, caret.Affinity);
            }
        }

        //Get the map -- if any -- used to map points in the view's edit buffer to the data buffer. The map is needed because the undo history
        //typically lives on the data buffer, but is used by the view on the edit buffer and a view (if any) on the data buffer. If there isn't
        //a contract that guarantees that the contents of the edit and databuffers are the same, undoing an action on the edit buffer view and
        //then undoing it on the data buffer view will cause cause the undo to try and restore caret/selection (in the data buffer) the coorinates
        //saved in the edit buffer. This isn't good.
        internal static IMapEditToData GetMap(ITextView view)
        {
            IMapEditToData map = null;
            if (view.TextViewModel.EditBuffer != view.TextViewModel.DataBuffer)
            {
                view.Properties.TryGetProperty(typeof(IMapEditToData), out map);
            }

            return map;
        }

        //Map point from a position in the edit buffer to a position in the data buffer (== if there is no map, otherwise ask the map).
        internal static int MapToData(IMapEditToData map, int point)
        {
            return (map != null) ? map.MapEditToData(point) : point;
        }

        //Map point from a position in the data buffer to a position in the edit buffer (== if there is no map, otherwise ask the map).
        internal static int MapToEdit(IMapEditToData map, int point)
        {
            return (map != null) ? map.MapDataToEdit(point) : point;
        }

        protected BeforeTextBufferChangeUndoPrimitive(ITextUndoHistory undoHistory, int caretIndex, PositionAffinity caretAffinity)
        {
            _undoHistory = undoHistory;
            _oldCaretIndex = caretIndex;
            _oldCaretAffinityByte = (byte)caretAffinity;
            _canUndo = true;
        }

        // Internal empty constructor for unit testing.
        internal BeforeTextBufferChangeUndoPrimitive() { }

        /// <summary>
        /// The <see cref="ITextView"/> that this <see cref="BeforeTextBufferChangeUndoPrimitive"/> is bound to.
        /// </summary>
        internal ITextView GetTextView()
        {
            ITextView view = null;
            _undoHistory.Properties.TryGetProperty(typeof(ITextView), out view);
            return view;
        }

        #region UndoPrimitive Members

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

            // Currently, no action is done on the redo.  To set the caret and selection for after a TextBuffer change redo, there is the AfterTextBufferChangeUndoPrimitive.
            // This Redo should not do anything with the caret and selection, because we only want to reset them after the TextBuffer change has occurred.
            // Therefore, we need to add this UndoPrimitive to the undo stack before the UndoPrimitive for the TextBuffer change.  On an undo, the TextBuffer changed UndoPrimitive
            // will fire it's Undo first, and than the Undo for this UndoPrimitive will fire.
            // However, on a redo, the Redo for this UndoPrimitive will be fired, and then the Redo for the TextBuffer change UndoPrimitive.  If we had set any caret placement/selection here (ie the new caret placement/selection), 
            // we may crash because the TextBuffer change has not occurred yet (ie you try to set the caret to be at CharacterIndex 1 when the TextBuffer is still empty).

            _canUndo = true;
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

            // Restore the old caret position and active selection
            var view = this.GetTextView();
            Debug.Assert(view == null || !view.IsClosed, "Attempt to undo/redo on a closed view?  This shouldn't happen.");
            if (view != null && !view.IsClosed)
            {
                UndoMoveCaretAndSelect(view, BeforeTextBufferChangeUndoPrimitive.GetMap(view));
                view.Caret.EnsureVisible();
            }

            _canUndo = false;
        }

        /// <summary>
        /// Move the caret and restore the selection as part of the Undo operation.
        /// </summary>
        protected virtual void UndoMoveCaretAndSelect(ITextView view, IMapEditToData map)
        {
            SnapshotPoint newCaret = new SnapshotPoint(view.TextSnapshot, MapToEdit(map, _oldCaretIndex));

            view.Caret.MoveTo(new VirtualSnapshotPoint(newCaret), (PositionAffinity)_oldCaretAffinityByte);
            view.Selection.Clear();
        }

        protected virtual int OldCaretVirtualSpaces
        {
            get { return 0; }
        }

        public override bool CanMerge(ITextUndoPrimitive older)
        {
            if (older == null)
            {
                throw new ArgumentNullException("older");
            }

            AfterTextBufferChangeUndoPrimitive olderPrimitive = older as AfterTextBufferChangeUndoPrimitive;
            // We can only merge with IUndoPrimitives of AfterTextBufferChangeUndoPrimitive type
            if (olderPrimitive == null)
            {
                return false;
            }

            return (olderPrimitive.CaretIndex == _oldCaretIndex) && (olderPrimitive.CaretVirtualSpace == OldCaretVirtualSpaces);
        }

        #endregion
    }

    /// <summary>
    /// The UndoPrimitive to take place on the Undo stack before a text buffer change. This is the general
    /// version of the primitive that handles all cases, including those involving selections and virtual space.
    /// </summary>
    internal class GeneralBeforeTextBufferChangeUndoPrimitive : BeforeTextBufferChangeUndoPrimitive
    {
        private int _oldCaretVirtualSpaces;
        private int _oldSelectionAnchorIndex;
        private int _oldSelectionAnchorVirtualSpaces;
        private int _oldSelectionActiveIndex;
        private int _oldSelectionActiveVirtualSpaces;
        private TextSelectionMode _oldSelectionMode;

        public GeneralBeforeTextBufferChangeUndoPrimitive(ITextUndoHistory undoHistory,
                                                          int oldCaretIndex,
                                                          PositionAffinity oldCaretAffinity,
                                                          int oldCaretVirtualSpaces,
                                                          int oldSelectionAnchorIndex,
                                                          int oldSelectionAnchorVirtualSpaces,
                                                          int oldSelectionActiveIndex,
                                                          int oldSelectionActiveVirtualSpaces,
                                                          TextSelectionMode oldSelectionMode)
            : base(undoHistory, oldCaretIndex, oldCaretAffinity)
        {
            _oldCaretVirtualSpaces = oldCaretVirtualSpaces;
            _oldSelectionAnchorIndex = oldSelectionAnchorIndex;
            _oldSelectionAnchorVirtualSpaces = oldSelectionAnchorVirtualSpaces;
            _oldSelectionActiveIndex = oldSelectionActiveIndex;
            _oldSelectionActiveVirtualSpaces = oldSelectionActiveVirtualSpaces;
            _oldSelectionMode = oldSelectionMode;
        }

        /// <summary>
        /// Move the caret and restore the selection as part of the Undo operation.
        /// </summary>
        protected override void UndoMoveCaretAndSelect(ITextView view, IMapEditToData map)
        {
            SnapshotPoint newCaret = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _oldCaretIndex));
            SnapshotPoint newAnchor = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _oldSelectionAnchorIndex));
            SnapshotPoint newActive = new SnapshotPoint(view.TextSnapshot, BeforeTextBufferChangeUndoPrimitive.MapToEdit(map, _oldSelectionActiveIndex));

            view.Caret.MoveTo(new VirtualSnapshotPoint(newCaret, _oldCaretVirtualSpaces), (PositionAffinity)_oldCaretAffinityByte);

            view.Selection.Mode = _oldSelectionMode;

            var virtualAnchor = new VirtualSnapshotPoint(newAnchor, _oldSelectionAnchorVirtualSpaces);
            var virtualActive = new VirtualSnapshotPoint(newActive, _oldSelectionActiveVirtualSpaces);

            // Buffer may have been changed by one of the listeners on the caret move event.
            virtualAnchor = virtualAnchor.TranslateTo(view.TextSnapshot);
            virtualActive = virtualActive.TranslateTo(view.TextSnapshot);

            view.Selection.Select(virtualAnchor, virtualActive);
        }

        protected override int OldCaretVirtualSpaces
        {
            get { return _oldCaretVirtualSpaces; }
        }
    }
}
