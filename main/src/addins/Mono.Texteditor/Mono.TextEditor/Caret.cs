// Caret.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;

namespace Mono.TextEditor
{
	public class Caret : IDisposable
	{
		DocumentLocation location;
		bool preserveSelection = false;
		bool isInInsertMode = true;
		bool autoScrollToCaret = true;
		bool isVisible = true;
		int  desiredColumn;
		Document document;
		TextEditor editor;
		
		public int Line {
			get {
				return location.Line;
			}
			set {
				if (location.Line != value) {
					DocumentLocation old = location;
					location.Line = value;
					SetColumn ();
					OnPositionChanged (new DocumentLocationEventArgs (old));
				}
			}
		}
		
		public int Column {
			get {
				return location.Column;
			}
			set {
				if (location.Column != value) {
					DocumentLocation old = location;
					location.Column = value;
					SetDesiredColumn ();
					OnPositionChanged (new DocumentLocationEventArgs (old));
				}
			}
		}
		
		public DocumentLocation Location {
			get {
				return location;
			}
			set {
				if (location != value) {
					DocumentLocation old = location;
					location = value;
					SetDesiredColumn ();
					OnPositionChanged (new DocumentLocationEventArgs (old));
				}
			}
		}

		public int Offset {
			get {
				int result = 0;
				if (Line < document.LineCount) {
					LineSegment line = document.GetLine (Line);
					if (line != null)
						result = line.Offset;
				}
				result += Column;
				return result;
			}
			set {
				int line   = document.OffsetToLineNumber (value);
				int column = value - document.GetLine (line).Offset;
				Location = new DocumentLocation (line, column);
			}
		}

		public bool PreserveSelection {
			get {
				return preserveSelection;
			}
			set {
				preserveSelection = value;
			}
		}

		public bool IsInInsertMode {
			get {
				return isInInsertMode;
			}
			set {
				isInInsertMode = value;
				OnModeChanged ();
			}
		}

		public bool AutoScrollToCaret {
			get {
				return autoScrollToCaret;
			}
			set {
				autoScrollToCaret = value;
			}
		}

		public bool IsVisible {
			get {
				return isVisible;
			}
			set {
				isVisible = value;
			}
		}
		
		public Caret (TextEditor editor, Document document)
		{
			this.editor = editor;
			this.document = document;
		}
		
		public void Dispose ()
		{
			this.document = null;
		}
		
		void SetDesiredColumn ()
		{
			LineSegment curLine = this.document.GetLine (this.Line);
			this.Column = System.Math.Min (curLine.EditableLength, System.Math.Max (0, this.Column));
			this.desiredColumn = curLine.GetVisualColumn (editor, document, this.Column);
		}
		
		void SetColumn ()
		{
			LineSegment curLine = this.document.GetLine (this.Line);
			this.location.Column = curLine.GetLogicalColumn (editor, this.document, this.desiredColumn);
		}
		
		public override string ToString ()
		{
			return String.Format ("[Caret: Location={0}, IsInInsertMode={1}]", 
			                      this.Location,
			                      this.isInInsertMode);
		}
		
		protected virtual void OnPositionChanged (DocumentLocationEventArgs args)
		{
			bool needUpdate = false;
			foreach (FoldSegment fold in this.document.GetFoldingsFromOffset (this.Offset)) {
				needUpdate |= fold.IsFolded;
				fold.IsFolded = false;
			}
			if (needUpdate) {
				document.RequestUpdate (new UpdateAll ());
				document.CommitDocumentUpdate ();
			}
				
			if (PositionChanged != null) 
				PositionChanged (this, args);
		}
		public event EventHandler<DocumentLocationEventArgs> PositionChanged;
		
		protected virtual void OnModeChanged ()
		{
			if (ModeChanged != null) 
				ModeChanged (this, EventArgs.Empty);
		}
		public event EventHandler ModeChanged;
	}
}
