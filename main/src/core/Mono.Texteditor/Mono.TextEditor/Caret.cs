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
	public class Caret
	{
		DocumentLocation location = new DocumentLocation (1, 1);
		
		bool isInInsertMode = true;
		bool autoScrollToCaret = true;
		
		CaretMode mode;
		
		public int Line {
			get {
				return location.Line;
			}
			set {
				if (location.Line != value) {
					if (value < DocumentLocation.MinLine)
						throw new ArgumentException ("Line < MinLine");
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
					if (value < DocumentLocation.MinColumn)
						throw new ArgumentException ("Column < MinColumn");
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
					if (value.Line < DocumentLocation.MinLine || value.Column < DocumentLocation.MinColumn)
						throw new ArgumentException ("invalid location: " + value);
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
				if (Line <= TextEditorData.Document.LineCount) {
					LineSegment line = TextEditorData.Document.GetLine (Line);
					if (line != null)
						result = line.Offset;
					result += System.Math.Min (Column - 1, line.EditableLength);
				}
				return result;
			}
			set {
				int line   = System.Math.Max (1, TextEditorData.Document.OffsetToLineNumber (value));
				var lineSegment = TextEditorData.Document.GetLine (line);
				int column = lineSegment != null ? value - lineSegment.Offset + 1 : 1;
				Location = new DocumentLocation (line, column);
			}
		}

		public bool PreserveSelection {
			get;
			set;
		}

		public bool IsInInsertMode {
			get {
				return CaretMode.Insert == mode;
			}
			set {
				mode = value? CaretMode.Insert: CaretMode.Block;
				OnModeChanged ();
			}
		}
		
		/// <summary>
		/// The current mode of the caret
		/// </summary>
		public CaretMode Mode {
			get {
				return mode;
			}
			set {
				mode = value;
				OnModeChanged ();
			}
		}

		public bool AutoScrollToCaret {
			get {
				return autoScrollToCaret;
			}
			set {
				if (value != autoScrollToCaret) {
					autoScrollToCaret = value;
					if (autoScrollToCaret)
						OnPositionChanged (new DocumentLocationEventArgs (Location));
				}
			}
		}

		public bool IsVisible {
			get;
			set;
		}

		public bool AllowCaretBehindLineEnd {
			get;
			set;
		}

		public int DesiredColumn {
			get;
			set;
		}
		
		public TextEditorData TextEditorData {
			get;
			private set;
		}
		
		public Caret (TextEditorData editor)
		{
			this.TextEditorData = editor;
			this.IsVisible = true;
			this.AllowCaretBehindLineEnd = false;
			this.DesiredColumn = DocumentLocation.MinColumn;
		}
		
		public void CheckCaretPosition ()
		{
			this.Line = System.Math.Min (this.Line, TextEditorData.Document.LineCount);
			if (!AllowCaretBehindLineEnd) {
				LineSegment curLine = TextEditorData.Document.GetLine (this.Line);
				this.Column = System.Math.Min (curLine.EditableLength + 1, this.Column);
			}
		}
		
		/// <summary>
		/// Activates auto scroll to caret on next caret move.
		/// </summary>
		public void ActivateAutoScrollWithoutMove ()
		{
			autoScrollToCaret = true;
		}
		
		public void SetToOffsetWithDesiredColumn (int desiredOffset)
		{
			DocumentLocation old = Location;
			
			int line   = TextEditorData.Document.OffsetToLineNumber (desiredOffset);
			int column = desiredOffset - TextEditorData.Document.GetLine (line).Offset + 1;
			location = new DocumentLocation (line, column);
			SetColumn ();
			OnPositionChanged (new DocumentLocationEventArgs (old));
		}
		
		void SetDesiredColumn ()
		{
			LineSegment curLine = TextEditorData.Document.GetLine (this.Line);
			if (curLine == null)
				return;
			
			if (!AllowCaretBehindLineEnd)
				this.Column = System.Math.Min (curLine.EditableLength + 1, System.Math.Max (DocumentLocation.MinColumn, this.Column));
			this.DesiredColumn = curLine.GetVisualColumn (TextEditorData, this.Column);
		}
		
		void SetColumn ()
		{
			LineSegment curLine = TextEditorData.Document.GetLine (this.Line);
			if (curLine == null)
				return;
			this.location.Column = curLine.GetLogicalColumn (TextEditorData, this.DesiredColumn);
			if (curLine.GetVisualColumn (TextEditorData, this.location.Column) < this.DesiredColumn) {
				this.location.Column = TextEditorData.GetNextVirtualColumn (Line, this.location.Column);
			} else {
				if (this.Column > curLine.EditableLength + 1) {
					this.location.Column = System.Math.Min (curLine.EditableLength + 1, System.Math.Max (DocumentLocation.MinColumn, this.Column));
					if (AllowCaretBehindLineEnd)
						this.location.Column = TextEditorData.GetNextVirtualColumn (Line, this.location.Column);
				}
			}
		}
		
		public void SetToDesiredColumn (int desiredColumn) 
		{
			DocumentLocation old = Location;
			this.DesiredColumn = desiredColumn;
			SetColumn ();
			OnPositionChanged (new DocumentLocationEventArgs (old));
		}
		
		public override string ToString ()
		{
			return String.Format ("[Caret: Location={0}, IsInInsertMode={1}]", 
			                      this.Location,
			                      this.isInInsertMode);
		}

		/// <summary>
		/// This method should be called after a fold segment is folded, to ensure
		/// that the caret is in a valid state.
		/// </summary>
		public void MoveCaretBeforeFoldings ()
		{
			int offset = this.Offset;
			foreach (FoldSegment fold in TextEditorData.Document.GetFoldingsFromOffset (this.Offset)) {
				if (fold.IsFolded)
					offset = System.Math.Min (offset, fold.Offset);
			}
			this.Offset = offset;
		}
		
		protected virtual void OnPositionChanged (DocumentLocationEventArgs args)
		{
			TextEditorData.Document.EnsureOffsetIsUnfolded (this.Offset);
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
	
	/// <summary>
	/// Possible visual modes for the caret
	/// </summary>
	public enum CaretMode
	{
		Insert,
		Block,
		Underscore
	}
}
