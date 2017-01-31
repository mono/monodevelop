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
using System.Linq;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	class CaretImpl : MonoDevelop.Ide.Editor.Caret
	{
		bool isInInsertMode = true;
		bool autoScrollToCaret = true;
		
		CaretMode mode;
		
		int line = DocumentLocation.MinLine;
		public override int Line {
			get {
				return line;
			}
			set {
				if (line != value) {
					if (value < DocumentLocation.MinLine)
						throw new ArgumentException ("Line < MinLine");
					var old = Location;
					line = value;
					CheckLine ();
					SetColumn ();
					UpdateCaretOffset ();
					OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.Movement));
				}
			}
		}
		
		int column = DocumentLocation.MinColumn;
		public override int Column {
			get {
				return column;
			}
			set {
				if (column != value) {
					if (value < DocumentLocation.MinColumn)
						throw new ArgumentException ("Column < MinColumn");
					var old = Location;
					column = value;
					CheckColumn ();
					SetDesiredColumn ();
					UpdateCaretOffset ();
					OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.Movement));
				}
			}
		}
		
		public override DocumentLocation Location {
			get {
				return new DocumentLocation (Line, Column);
			}
			set {
				if (Location != value) {
					if (value.Line < DocumentLocation.MinLine || value.Column < DocumentLocation.MinColumn)
						throw new ArgumentException ("invalid location: " + value);
					DocumentLocation old = Location;
					line = value.Line;
					column = value.Column;
					CheckLine ();
					CheckColumn ();
					SetDesiredColumn ();
					UpdateCaretOffset ();
					OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.Movement));
				}
			}
		}

		ITextSourceVersion offsetVersion;
		int caretOffset;
		public override int Offset {
			get {
				return caretOffset;
			}
			set {
				if (caretOffset == value)
					return;
				if (caretOffset < 0)
					throw new InvalidOperationException ($"Caret offset must be >= 0 tried to set {value}");
				if (caretOffset > TextEditorData.Length)
					throw new InvalidOperationException ($"Caret offset must be < Length {TextEditorData.Length} but was {value}");
				DocumentLocation old = Location;
				caretOffset = value;
				offsetVersion = TextEditorData.Document.Version;
				line = System.Math.Max (1, TextEditorData.Document.OffsetToLineNumber (value));
				var lineSegment = TextEditorData.Document.GetLine (line);
				column = lineSegment != null ? value - lineSegment.Offset + 1 : 1;
				
				CheckLine ();
				CheckColumn ();
				SetDesiredColumn ();
				OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.Movement));
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

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Mono.TextEditor.Caret"/> will listen to text chagne events to update the caret position.
		/// </summary>
		public bool AutoUpdatePosition {
			get;
			set;
		}

		public CaretImpl (TextEditorData editor)
		{
			TextEditorData = editor;
			IsVisible = true;
			AllowCaretBehindLineEnd = false;
			DesiredColumn = DocumentLocation.MinColumn;
			AutoUpdatePosition = true;
		}

		/// <summary>
		/// Activates auto scroll to caret on next caret move.
		/// </summary>
		public void ActivateAutoScrollWithoutMove ()
		{
			autoScrollToCaret = true;
		}

		void CheckLine ()
		{
			if (line > TextEditorData.Document.LineCount) {
				line = TextEditorData.Document.LineCount;
				UpdateCaretOffset ();
			}
		}

		void CheckColumn ()
		{
			var curLine = TextEditorData.Document.GetLine (Line);

			if (TextEditorData.HasIndentationTracker && TextEditorData.Options.IndentStyle == IndentStyle.Virtual && curLine.Length == 0) {
				if (column > DocumentLocation.MinColumn) {
					var indentColumn = TextEditorData.GetVirtualIndentationColumn (Location);
					if (column < indentColumn) {
						column = indentColumn;
						UpdateCaretOffset ();
						return;
					}
					if (column == indentColumn)
						return;
				}
			}

			if (!AllowCaretBehindLineEnd) {
				var max = curLine.Length + 1;
				if (column > max) {
					column = max;
					UpdateCaretOffset ();
				}
			}
		}

		public void SetToOffsetWithDesiredColumn (int desiredOffset)
		{
			DocumentLocation old = Location;

			int desiredLineNumber = TextEditorData.Document.OffsetToLineNumber (desiredOffset);
			var desiredLine = TextEditorData.Document.GetLine (desiredLineNumber);
			int newColumn = desiredOffset - desiredLine.Offset + 1;
			if (desiredLine.Length + 1 < Column && newColumn == 1) {
				if (TextEditorData.HasIndentationTracker && TextEditorData.Options.IndentStyle == IndentStyle.Virtual)
					newColumn = TextEditorData.GetVirtualIndentationColumn (desiredLineNumber, 1);
			}
			
			line = desiredLineNumber;
			column = newColumn;
			var logicalDesiredColumn = desiredLine.GetLogicalColumn (TextEditorData, DesiredColumn);

			if (logicalDesiredColumn <= desiredLine.Length + 1) {
				int possibleOffset = TextEditorData.LocationToOffset (desiredLineNumber, logicalDesiredColumn);
				if (!TextEditorData.Document.GetFoldingsFromOffset (possibleOffset).Any (f => f.IsCollapsed))
					column = logicalDesiredColumn;
			} else {
				column = System.Math.Max (newColumn, desiredLine.Length + 1);
			}

			UpdateCaretOffset ();
			OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.Movement));
		}

		void SetDesiredColumn ()
		{
			var curLine = TextEditorData.Document.GetLine (Line);
			if (curLine == null)
				return;
			DesiredColumn = curLine.GetVisualColumn (TextEditorData, Column);
		}

		void SetColumn ()
		{
			var curLine = TextEditorData.Document.GetLine (Line);
			if (curLine == null)
				return;
			column = System.Math.Max (DocumentLocation.MinColumn, Column);
			column = curLine.GetLogicalColumn (TextEditorData, DesiredColumn);
			if (TextEditorData.HasIndentationTracker && TextEditorData.Options.IndentStyle == IndentStyle.Virtual && curLine.GetVisualColumn (TextEditorData, column) < DesiredColumn) {
				column = TextEditorData.GetVirtualIndentationColumn (Line, column);
			} else {
				if (!AllowCaretBehindLineEnd && Column > curLine.Length + 1)
					column = System.Math.Min (curLine.Length + 1, column);
			}
		}
		
		public void SetToDesiredColumn (int desiredColumn) 
		{
			var old = Location;
			DesiredColumn = desiredColumn;
			SetColumn ();
			OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.Movement));
		}
		
		public override string ToString ()
		{
			return String.Format ("[Caret: Location={0}, IsInInsertMode={1}]", 
			                      Location,
			                      isInInsertMode);
		}

		/// <summary>
		/// This method should be called after a fold segment is folded, to ensure
		/// that the caret is in a valid state.
		/// </summary>
		public void MoveCaretBeforeFoldings ()
		{
			int offset = Offset;
			foreach (FoldSegment fold in TextEditorData.Document.GetFoldingsFromOffset (Offset)) {
				if (fold.IsCollapsed)
					offset = System.Math.Min (offset, fold.Offset);
			}
			Offset = offset;
		}

		protected override void OnPositionChanged (CaretLocationEventArgs args)
		{
			TextEditorData.Document.EnsureOffsetIsUnfolded (Offset);
			base.OnPositionChanged (args);
		}
		
		protected virtual void OnModeChanged ()
		{
			if (ModeChanged != null) 
				ModeChanged (this, EventArgs.Empty);
		}
		public event EventHandler ModeChanged;

		public void UpdateCaretOffset ()
		{
			int result = 0;
			var doc = TextEditorData.Document;
			if (doc == null)
				return;
			if (Line <= doc.LineCount) {
				DocumentLine line = doc.GetLine (Line);
				if (line != null) {
					result = line.Offset;
					result += System.Math.Min (Column - 1, line.Length);
				}
			}
			caretOffset = System.Math.Max(0, result);
			offsetVersion = doc.Version;
		}

		internal void UpdateCaretPosition (TextChangeEventArgs e)
		{
			//if (e.AnchorMovementType == AnchorMovementType.BeforeInsertion && caretOffset == e.Offset) {
			//	offsetVersion = TextEditorData.Version;
			//	return;
			//}
			var curVersion = TextEditorData.Version;
			if (offsetVersion == null) {
				offsetVersion = curVersion;
				return;
			}
			var newOffset = offsetVersion.MoveOffsetTo (curVersion, caretOffset);
			offsetVersion = curVersion;
			if (newOffset == caretOffset || !AutoUpdatePosition)
				return;
			DocumentLocation old = Location;
			var newLocation = TextEditorData.OffsetToLocation (newOffset);
			int newColumn = newLocation.Column;
			
			var curLine = TextEditorData.GetLine (newLocation.Line);
			if (TextEditorData.HasIndentationTracker && TextEditorData.Options.IndentStyle == IndentStyle.Virtual && curLine.Length == 0) {
				var indentColumn = TextEditorData.GetVirtualIndentationColumn (newLocation);
				if (column == indentColumn) {
					newColumn = indentColumn;
				}
			}
			if (AllowCaretBehindLineEnd) {
				if (curLine != null && column > curLine.Length)
					newColumn = column;
			}
			line = newLocation.Line;
			column = newColumn;

			SetDesiredColumn ();
			UpdateCaretOffset ();
			OnPositionChanged (new CaretLocationEventArgs (old, CaretChangeReason.BufferChange));
		}

		public void SetDocument (TextDocument doc)
		{
			line = column = 1;
			offsetVersion = doc.Version;
			caretOffset = 0;
		}
	}
	
	/// <summary>
	/// Possible visual modes for the caret
	/// </summary>
	enum CaretMode
	{
		Insert,
		Block,
		Underscore
	}
}
