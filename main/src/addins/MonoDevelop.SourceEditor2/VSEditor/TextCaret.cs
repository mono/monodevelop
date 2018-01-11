//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;

namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    internal class TextCaret : ITextCaret
    {
        private TextArea _textEditor;
        private ITextView _textView;

        private VirtualSnapshotPoint _insertionPoint;
        private PositionAffinity _caretAffinity;

        public TextCaret(TextArea textEditor)
        {
            _textEditor = textEditor;
            _textView = textEditor;

            // Set up initial values
            _caretAffinity = PositionAffinity.Successor;
            _insertionPoint = new VirtualSnapshotPoint(new SnapshotPoint(textEditor.TextSnapshot, 0));

            _textEditor.Caret.PositionChanged += ImmediateCaretPositionChanged;
        }

        void ImmediateCaretPositionChanged(object sender, CaretLocationEventArgs args)
        {
            // MD doesn't fire textEditor.CaretPositionChanged until after the command has gone completely through the command chain. 
            //   Too much VS stuff depends on it getting updated earlier, so we'll use this event which fires earlier.
            int position = _textEditor.Caret.Offset;
            VirtualSnapshotPoint vsp = new VirtualSnapshotPoint(_textView.TextSnapshot, position);
            
            _insertionPoint = vsp;
            if (args.CaretChangeReason == CaretChangeReason.Movement)
            {
                SnapshotPoint snapshotPoint = new SnapshotPoint(_textView.TextSnapshot, position);
                IMappingPoint mappingPoint = _textView.BufferGraph.CreateMappingPoint(snapshotPoint, PointTrackingMode.Positive);
                CaretPosition newCaretPosition = new CaretPosition(vsp, mappingPoint, _caretAffinity);
                CaretPositionChangedEventArgs eventArgs = new CaretPositionChangedEventArgs(_textView, Position, newCaretPosition);

                PositionChanged?.Invoke(this, eventArgs);
            }
        }

        public double Bottom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ITextViewLine ContainingTextViewLine
        {
            get
            {
                return _textView.GetTextViewLineContainingBufferPosition (Position.VirtualBufferPosition.Position);
            }
        }

        public double Height
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool InVirtualSpace
        {
            get
            {
                ITextSnapshotLine snapshotLine = _textView.TextBuffer.CurrentSnapshot.GetLineFromPosition(Position.BufferPosition);

                return _textEditor.Caret.Column > snapshotLine.Length + 1;
            }
        }

        public bool IsHidden
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public double Left
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool OverwriteMode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public CaretPosition Position
        {
            get
            {
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
                VirtualSnapshotPoint insertionPointInLatestSnapshot = _insertionPoint.TranslateTo(_insertionPoint.Position.Snapshot.TextBuffer.CurrentSnapshot, PointTrackingMode.Positive);
                return new CaretPosition(insertionPointInLatestSnapshot,
                                         _textView.BufferGraph.CreateMappingPoint(insertionPointInLatestSnapshot.Position, PointTrackingMode.Positive),
                                         _caretAffinity);
#endif
            }
        }

        public double Right
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double Top
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double Width
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<CaretPositionChangedEventArgs> PositionChanged;

        public void EnsureVisible()
        {
            _textEditor.ScrollTo(_textEditor.Caret.Offset);
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition)
        {
            this.InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), PositionAffinity.Successor, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition)
        {
            this.InternalMoveTo(bufferPosition, PositionAffinity.Successor, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(ITextViewLine textLine)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        {
            this.InternalMoveTo(bufferPosition, caretAffinity, true, true, true);

            return this.Position;
        }

        public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition)
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveToNextCaretPosition()
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveToPreferredCoordinates()
        {
            throw new NotImplementedException();
        }

        public CaretPosition MoveToPreviousCaretPosition()
        {
            throw new NotImplementedException();
        }

        private void InternalMoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
        {
            int requestedPosition = bufferPosition.Position;
            ITextSnapshotLine snapshotLine = _textView.TextSnapshot.GetLineFromPosition(requestedPosition);
            int line = snapshotLine.LineNumber + 1;

            int col;
            if (bufferPosition.IsInVirtualSpace)
            {
                col = bufferPosition.VirtualSpaces;
            }
            else 
            {
                col = requestedPosition - snapshotLine.Start + 1;
            }

            _textEditor.SetCaretTo (line, col, false, false);
        }
    }
}

