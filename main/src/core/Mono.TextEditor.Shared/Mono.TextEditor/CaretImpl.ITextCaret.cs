//
// CaretImpl.ITextCaret.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	partial class CaretImpl : ITextCaret
	{
		MonoTextEditor TextEditor => TextEditorData.Parent;

		VirtualSnapshotPoint insertionPoint;
		PositionAffinity _caretAffinity;

		ITextViewLine ITextCaret.ContainingTextViewLine {
			get {
				var position = ((ITextCaret)this).Position.BufferPosition;
				var line = TextEditor.GetTextViewLineContainingBufferPosition (position);
				return line;
			}
		}

		double ITextCaret.Left => TextEditor.TextArea.TextViewMargin.caretX;

		double ITextCaret.Width => TextEditor.TextArea.TextViewMargin.charWidth;

		double ITextCaret.Right => TextEditor.TextArea.TextViewMargin.caretX + TextEditor.TextArea.TextViewMargin.charWidth;

		double ITextCaret.Top => TextEditor.TextArea.TextViewMargin.caretY;

		double ITextCaret.Height => TextEditor.LineHeight;

		double ITextCaret.Bottom => TextEditor.TextArea.TextViewMargin.caretY + TextEditor.LineHeight;

		CaretPosition ITextCaret.Position {
			get {
#if TARGET_VS
                //In theory the _insertion point is always at the same snapshot at the _wpfTextView but there could be cases
                //where someone is using the position in a classifier that is using the caret position in the classificaiton changed event.
                //In that case return the old insertion point.
                return new CaretPosition(_insertionPoint,
                                         _textView.BufferGraph.CreateMappingPoint(_insertionPoint.Position, PointTrackingMode.Positive),
                                         _caretAffinity);
#else
				// MD doesn't update the caret location until after the command has gone completely through the command chain. Too much VS stuff depends on it getting updated earlier.
				//   Thus, I'm going to ensure this is returning a position based on the current snapshot, ignoring/breaking the scenario outlined in the above comment.
				VirtualSnapshotPoint insertionPointInLatestSnapshot = insertionPoint.TranslateTo (insertionPoint.Position.Snapshot.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);
				return new CaretPosition (insertionPointInLatestSnapshot,
										 TextEditor.BufferGraph.CreateMappingPoint (insertionPointInLatestSnapshot.Position, PointTrackingMode.Positive),
										 _caretAffinity);
#endif
			}
		}


		bool ITextCaret.OverwriteMode => !IsInInsertMode;

		bool ITextCaret.InVirtualSpace {
			get {
				var snapshotLine = TextEditor.TextBuffer.CurrentSnapshot.GetLineFromPosition (((ITextCaret)this).Position.BufferPosition);

				return TextEditor.Caret.Column > snapshotLine.Length + 1;
			}
		}

		bool ITextCaret.IsHidden {
			get {
				return !IsVisible;
			}
			set {
				IsVisible = !value;
			} 
		}

		DocumentLocation oldCaretLocation;
		void PositionChanged_ITextCaret (CaretLocationEventArgs args)
		{
			//Some unit tests don't initialize full UI representation of MonoTextEditor
			//which means they don't depend on ITextCaret implementation, so we can return here
			//If something is using MonoTextEditor directly(e.g. DiffView) and is not initializing ITextView
			//TextBuffer is null, in that case don't depend on ITextCaret implementation, so we can return here
			if (TextEditor?.TextBuffer == null)
				return;
			// MD doesn't fire textEditor.CaretPositionChanged until after the command has gone completely through the command chain. 
			//   Too much VS stuff depends on it getting updated earlier, so we'll use this event which fires earlier.
			int position = TextEditor.Caret.Offset;
			VirtualSnapshotPoint vsp = new VirtualSnapshotPoint (TextEditor.TextSnapshot, position);

			insertionPoint = vsp;
			if (args.CaretChangeReason == CaretChangeReason.Movement) {
				oldCaretLocation = args.Location;
				var oldOffset = TextEditor.LocationToOffset (args.Location);
				var snapshotPoint = new SnapshotPoint (TextEditor.TextSnapshot, oldOffset);
				var mappingPoint = TextEditor.BufferGraph.CreateMappingPoint (snapshotPoint, PointTrackingMode.Positive);
				var oldCaretPosition = new CaretPosition (vsp, mappingPoint, _caretAffinity);
				var eventArgs = new CaretPositionChangedEventArgs (TextEditor, oldCaretPosition, ((ITextCaret)this).Position);

				ITextCaret_PositionChanged?.Invoke (this, eventArgs);
			}

			// Synchronize the MultiSelectionBroker with Caret.
			// In VS Editor 15.8 the MultiSelectionBroker is the single source of truth about carets and selections 
			// (no selection / single caret, no selection / multiple carets, simple selection, block selection, multiple selections).
			// In our world, we still have our own Caret and Selection, so when our Caret moves, we need to synchronize 
			// the MultiSelectionBroker to our values, and when the MultiSelectionBroker changes 
			// (e.g. as a result of EditorOperations such as InsertNewLine), we need to synchronize our caret and selection 
			// to the MultiSelectionBroker values.
			// Note that EditorOperations only updates the MultiSelectionBroker, it no longer moves caret or selection 
			// (since in Editor 15.8 the Caret and Selection are shims implemented in terms of MultiSelectionBroker)
			// TODO: synchronize all our selections as well.
			TextEditorData.Parent.MultiSelectionBroker.PerformActionOnAllSelections (transformer => {
				transformer.MoveTo (vsp, select: false, PositionAffinity.Successor);
			});
		}

		event EventHandler<CaretPositionChangedEventArgs> ITextCaret_PositionChanged;
		event EventHandler<CaretPositionChangedEventArgs> ITextCaret.PositionChanged {
			add { ITextCaret_PositionChanged += value; }
			remove { ITextCaret_PositionChanged -= value; }
		}

		void ITextCaret.EnsureVisible ()
		{
			TextEditor.ScrollToCaret ();
		}


		CaretPosition ITextCaret.MoveTo (ITextViewLine textLine, double xCoordinate)
		{
			Line = textLine.Start;
			// TODO: xCoordinate - is that just visual or does it have an impact on the column ?
			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition)
		{
			Line = textLine.Start;
			// TODO: xCoordinate - is that just visual or does it have an impact on the column ?
			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (ITextViewLine textLine)
		{
			Line = textLine.Start;
			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (SnapshotPoint bufferPosition)
		{
			this.InternalMoveTo (new VirtualSnapshotPoint (bufferPosition), PositionAffinity.Successor, true, true, true);

			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
		{
			this.InternalMoveTo (new VirtualSnapshotPoint (bufferPosition), caretAffinity, true, true, true);

			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
		{
			this.InternalMoveTo (new VirtualSnapshotPoint (bufferPosition), caretAffinity, captureHorizontalPosition, true, true);

			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (VirtualSnapshotPoint bufferPosition)
		{
			this.InternalMoveTo (bufferPosition, PositionAffinity.Successor, true, true, true);

			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity)
		{
			this.InternalMoveTo (bufferPosition, caretAffinity, true, true, true);

			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveTo (VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
		{
			this.InternalMoveTo (bufferPosition, caretAffinity, captureHorizontalPosition, true, true);

			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveToNextCaretPosition ()
		{
			// TODO: Implement me - not sure if we've a 'next' position. What should this be ?
			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveToPreferredCoordinates ()
		{
			TextEditor.GetTextEditorData ().FixVirtualIndentation ();
			return ((ITextCaret)this).Position;
		}

		CaretPosition ITextCaret.MoveToPreviousCaretPosition ()
		{
			Location = oldCaretLocation;
			return ((ITextCaret)this).Position;
		}

        void InternalMoveTo (VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
		{
			int requestedPosition = bufferPosition.Position;
			ITextSnapshotLine snapshotLine = TextEditor.TextSnapshot.GetLineFromPosition (requestedPosition);
			int line = snapshotLine.LineNumber + 1;

			int col;
			if (bufferPosition.IsInVirtualSpace) {
				col = bufferPosition.VirtualSpaces;

				if (!TextEditor.Options.TabsToSpaces) {
					col = col / TextEditor.Options.TabSize;
				}
			} else {
				col = requestedPosition - snapshotLine.Start;
			}

			col += 1;

			TextEditor.SetCaretTo (line, col, false, false);
		}
	}
}