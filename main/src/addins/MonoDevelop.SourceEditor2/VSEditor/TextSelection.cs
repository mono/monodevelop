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
		private Mono.TextEditor.TextArea _textEditor;
		private ITextView _textView;

		public TextSelection(Mono.TextEditor.TextArea textArea)
		{
			_textEditor = textArea;
			_textView = textArea;

			_textEditor.SelectionChanged += OnSelectionChanged;
        }

        void OnSelectionChanged(object s, EventArgs args)
        {
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

        public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans
        {
            get
            {
                throw new NotImplementedException();
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
