//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using MonoDevelop.Ide.Editor;

namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    internal class TextSelection : ITextSelection
    {
		private Mono.TextEditor.MonoTextEditor _textEditor;
        private ITextView _textView;
		private List<VirtualSnapshotSpan> _virtualSelectedSpans;

		public TextSelection(Mono.TextEditor.MonoTextEditor textArea)
        {
            _textEditor = textArea;
            _textView = textArea;

            _textEditor.SelectionChanged += OnSelectionChanged;
        }

        void OnSelectionChanged(object s, EventArgs args)
        {
			_virtualSelectedSpans = null;
            // TODO: push both ways?
            SelectionChanged?.Invoke(this, new EventArgs());
        }

        public bool ActivationTracksFocus
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

        public VirtualSnapshotPoint ActivePoint
        {
            get
            {
                int offset = _textEditor.SelectionLead;
                if (offset == -1)
                    offset = _textEditor.SelectionRange.Offset;  // Selection is empty

                SnapshotPoint snapshotPoint = new SnapshotPoint(_textView.TextSnapshot, offset);
                VirtualSnapshotPoint virtualPoint = new VirtualSnapshotPoint(snapshotPoint);

                return virtualPoint;
            }
        }

        public VirtualSnapshotPoint AnchorPoint
        {
            get
            {
                int offset = _textEditor.SelectionAnchor;
                if (offset == -1)
                    offset = _textEditor.SelectionRange.Offset;  // Selection is empty

                SnapshotPoint snapshotPoint = new SnapshotPoint(_textView.TextSnapshot, offset);
                VirtualSnapshotPoint virtualPoint = new VirtualSnapshotPoint(snapshotPoint);

                return virtualPoint;
            }
        }

        public VirtualSnapshotPoint End
        {
            get
            {
                SnapshotPoint snapshotPoint = new SnapshotPoint(_textView.TextSnapshot, _textEditor.SelectionRange.EndOffset);
                VirtualSnapshotPoint virtualPoint = new VirtualSnapshotPoint(snapshotPoint);

                return virtualPoint;
            }
        }

        public bool IsActive
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

        public bool IsReversed
        {
            get
            {
                return (ActivePoint < AnchorPoint);
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (ActivePoint == AnchorPoint);
            }
        }

        public TextSelectionMode Mode
        {
            get
            {
                return _textEditor.SelectionMode == SelectionMode.Normal ? TextSelectionMode.Stream : TextSelectionMode.Box;

            }
            set
            {
                // TODO: MONO: Not sure what to do here as there isn't a SetSelectionMode on the editor. I see an internal
                //  IEditorActionHost.SwitchCaretMode method, but I can't get to that. 
                // _textEditor.SetSelectionMode = (value == TextSelectionMode.Stream ? SelectionMode.Normal : SelectionMode.Block);
            }
        }

        public NormalizedSnapshotSpanCollection SelectedSpans
        {
            get
            {
                if (_textEditor.SelectionMode == SelectionMode.Normal)
                {
                    int start = _textEditor.SelectionRange.Offset;
                    int len = _textEditor.SelectionRange.Length;

                    SnapshotSpan selectedRange = new SnapshotSpan(_textView.TextSnapshot, start, len);
                    return new NormalizedSnapshotSpanCollection(selectedRange);
                }
                else
                {
                    IList<SnapshotSpan> spans = new List<SnapshotSpan>();
                    foreach (MonoDevelop.Ide.Editor.Selection curSelection in new MonoDevelop.Ide.Editor.Selection[] { _textEditor.MainSelection })
                    {
                        for (int curLineIndex = curSelection.MinLine; curLineIndex <= curSelection.MaxLine; curLineIndex++)
                        {
                            int start = _textEditor.LocationToOffset(curLineIndex, curSelection.Start.Column);
                            int end = _textEditor.LocationToOffset(curLineIndex, curSelection.End.Column);

                            spans.Add(new SnapshotSpan(_textView.TextSnapshot, start, end - start));
                        }
                    }

                    return new NormalizedSnapshotSpanCollection(spans);
                }
            }
        }

        public VirtualSnapshotPoint Start
        {
            get
            {
                SnapshotPoint snapshotPoint = new SnapshotPoint(_textView.TextSnapshot, _textEditor.SelectionRange.Offset);
                VirtualSnapshotPoint virtualPoint = new VirtualSnapshotPoint(snapshotPoint);

                return virtualPoint;
            }
        }

        public VirtualSnapshotSpan StreamSelectionSpan
        {
            get
            {
                return new VirtualSnapshotSpan(this.Start, this.End);
            }
        }

        public ITextView TextView
        {
            get
            {
                return _textView;
            }
        }

		private void EnsureVirtualSelectedSpans ()
		{
			if (_virtualSelectedSpans == null) {
				_virtualSelectedSpans = new List<VirtualSnapshotSpan> ();
				if (this.IsEmpty) {
					VirtualSnapshotPoint caretPoint = this.ActivePoint; //== this.AnchorPoint
					_virtualSelectedSpans.Add (new VirtualSnapshotSpan (caretPoint, caretPoint));
				} else {
					if (this.Mode == TextSelectionMode.Box) {
						SnapshotPoint current = this.Start.Position;
						VirtualSnapshotPoint end = this.End;

						do {
							ITextViewLine line = _textView.GetTextViewLineContainingBufferPosition (current);

							VirtualSnapshotSpan? span = this.GetSelectionOnTextViewLine (line);
							if (span.HasValue)
								_virtualSelectedSpans.Add (span.Value);

							if (line.LineBreakLength == 0 && line.IsLastTextViewLineForSnapshotLine)
								break;      //Just processed last text view line in buffer.

							current = line.EndIncludingLineBreak;
						}
						while ((current.Position <= end.Position.Position) ||                               //Continue while the virtual space version of current
							   (end.IsInVirtualSpace && (current.Position == end.Position.Position)));      //is less than the virtual space position of the end of selection.
					} else {
						_virtualSelectedSpans.Add (new VirtualSnapshotSpan (this.Start, this.End));
					}
				}
			}
		}

		public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans
        {
            get
            {
                // Our code doesn't invalidate this in all places, so for now ensure we invalidate always
                // to avoid hard to diagnose bugs.
                // This isn't called often so is not a perf concern.
				_virtualSelectedSpans = null;
				EnsureVirtualSelectedSpans ();
				return new ReadOnlyCollection<VirtualSnapshotSpan> (_virtualSelectedSpans);
            }
        }

        public event EventHandler SelectionChanged;

        public void Clear()
        {
            _textEditor.ClearSelection();
        }

        public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line)
        {
            throw new NotImplementedException();
        }

        public void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            _textEditor.SetSelection(anchorPoint.Position, activePoint.Position);
        }

        public void Select(SnapshotSpan selectionSpan, bool isReversed)
        {
            VirtualSnapshotPoint start = new VirtualSnapshotPoint(selectionSpan.Start);
            VirtualSnapshotPoint end = new VirtualSnapshotPoint(selectionSpan.End);

            if (isReversed)
            {
                this.Select(end, start);
            }
            else
            {
                this.Select(start, end);
            }
        }
    }
}
