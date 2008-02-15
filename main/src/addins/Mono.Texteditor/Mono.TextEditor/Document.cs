// Document.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public class Document : AbstractBuffer
	{
		IBuffer      buffer;
		LineSplitter splitter;
		SyntaxMode   syntaxMode = null;
		string       eol = null;
		
		string mimeType;
		string fileName;
		
		/// <value>
		/// The eol mark used in this document - it's taken from the first line in the document,
		/// if no eol mark is found it's using the default (Environment.NewLine).
		/// The value is saved, even when all lines are deleted the eol marker will still be the old eol marker.
		/// </value>
		public string EolMarker {
			get {
				if (eol == null && splitter.LineCount > 0) {
					LineSegment line = splitter.Get (0);
					if (line.DelimiterLength > 0) 
						eol = buffer.GetTextAt (line.EditableLength, line.DelimiterLength);
				}
				return !String.IsNullOrEmpty (eol) ? eol : Environment.NewLine;
			}
		}
		
		public string MimeType {
			get {
				return mimeType;
			}
			set {
				mimeType        = value;
				this.SyntaxMode = SyntaxModeService.GetSyntaxMode (value);
			}
		}
		
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}
		
		public SyntaxMode SyntaxMode {
			get {
				return syntaxMode;
			}
			set {
				syntaxMode = value;
			}
		}
		
		public Document()
		{
			buffer = new GapBuffer ();
			splitter = new LineSplitter (buffer);
		}
		
		#region Buffer implementation
		public override int Length {
			get {
				return this.buffer.Length;
			}
		}
		
		public override string Text {
			get {
				return this.buffer.Text;
			}
			set {
				splitter.Clear ();
				int oldLength = Length;
				ReplaceEventArgs args = new ReplaceEventArgs (0, oldLength, new StringBuilder (value));
				this.OnTextReplacing (args);
				this.buffer.Text = value;
				splitter.TextReplaced (this, args);
				if (this.syntaxMode != null)
					Mono.TextEditor.Highlighting.SyntaxModeService.StartUpdate (this, this.syntaxMode, 0, value.Length);
				Mono.TextEditor.Highlighting.SyntaxModeService.WaitForUpdate ();
				this.OnTextReplaced (args);
			}
		}
		
		public void Insert (int offset, string value)
		{
			Insert (offset, new StringBuilder (value));
		}
		
		public void Replace (int offset, int count, string value)
		{
			Replace (offset, count, new StringBuilder (value));
		}
		
		public override void Replace (int offset, int count, StringBuilder value)
		{
//			Debug.Assert (count >= 0);
//			Debug.Assert (0 <= offset && offset + count <= Length);
			
			ReplaceEventArgs args = new ReplaceEventArgs (offset, count, value);
			OnTextReplacing (args);
			
			if (!isInUndo) {
				UndoOperation operation = new UndoOperation (args, GetTextAt (offset, count));
				if (currentAtomicOperation != null) {
					currentAtomicOperation.Add (operation);
				} else {
					undoStack.Push (operation);
				}
				redoStack.Clear ();
			}
			
			buffer.Replace (offset, count, value);
			OnTextReplaced (args);
			splitter.TextReplaced (this, args);
			if (this.syntaxMode != null)
				Mono.TextEditor.Highlighting.SyntaxModeService.StartUpdate (this, this.syntaxMode, offset, offset + count);
		}
		
		public string GetTextBetween (int startOffset, int endOffset)
		{
			return buffer.GetTextAt (startOffset, endOffset - startOffset);
		}
		
		public override string GetTextAt (int offset, int count)
		{
			return buffer.GetTextAt (offset, count);
		}
		
		public override char GetCharAt (int offset)
		{
			return buffer.GetCharAt (offset);
		}
		
		protected virtual void OnTextReplaced (ReplaceEventArgs args)
		{
			if (TextReplaced != null)
				TextReplaced (this, args);
		}
		public event EventHandler<ReplaceEventArgs> TextReplaced;
		
		protected virtual void OnTextReplacing (ReplaceEventArgs args)
		{
			if (TextReplacing != null)
				TextReplacing (this, args);
		}
		public event EventHandler<ReplaceEventArgs> TextReplacing;
		
		#endregion
		
		#region Line Splitter operations
		public IEnumerable<LineSegment> Lines {
			get {
				return splitter.Lines;
			}
		}
		
		public int LineCount {
			get {
				return splitter.LineCount;
			}
		}
		
		public int LocationToOffset (int line, int column)
		{
			return LocationToOffset (new DocumentLocation (line, column));
		}
		
		public int LocationToOffset (DocumentLocation location)
		{
			if (location.Line >= this.splitter.LineCount) 
				return -1;
			LineSegment line = GetLine (location.Line);
			return System.Math.Min (Length, line.Offset + System.Math.Min (line.Length, location.Column));
		}
		
		public DocumentLocation OffsetToLocation (int offset)
		{
			int lineNr = splitter.OffsetToLineNumber (offset);
			if (lineNr < 0)
				return DocumentLocation.Empty;
			LineSegment line = GetLine (lineNr);
			return new DocumentLocation (lineNr, System.Math.Min (line.Length, offset - line.Offset));
		}
		
		public LineSegment GetLine (int lineNumber)
		{
			return splitter.Get (lineNumber);
		}
		
		public LineSegment GetLineByOffset (int offset)
		{
			return splitter.GetLineByOffset (offset);
		}
		
		public int OffsetToLineNumber (int offset)
		{
			return splitter.OffsetToLineNumber (offset);
		}
		
		#endregion
		
		#region Undo/Redo operations
		class UndoOperation 
		{
			ReplaceEventArgs args;
			string text;
			
			protected UndoOperation()
			{
			}
			
			public UndoOperation (ReplaceEventArgs args, string text)
			{
				this.args = args;
				this.text = text;
			}
			public virtual void Undo (Document doc)
			{
				if (args.Value != null && args.Value.Length > 0)
					doc.Remove (args.Offset, args.Value.Length);
				if (!String.IsNullOrEmpty (text))
					doc.Insert (args.Offset, text);
			}
			
			public virtual void Redo (Document doc)
			{
				doc.Replace (args.Offset, args.Count, args.Value);
			}
		}
		
		class AtomicUndoOperation : UndoOperation
		{
			List<UndoOperation> operations = new List<UndoOperation> ();
			
			public void Add (UndoOperation operation)
			{
				operations.Add (operation);
			}
			
			public override void Undo (Document doc)
			{
				for (int i = operations.Count - 1; i >= 0; i--) {
					operations[i].Undo (doc);
				}
			}
			
			public override void Redo (Document doc)
			{
				foreach (UndoOperation operation in this.operations) {
					operation.Redo (doc);
				}
			}
			
		}
		
		bool isInUndo = false;
		Stack<UndoOperation> undoStack = new Stack<UndoOperation> ();
		Stack<UndoOperation> redoStack = new Stack<UndoOperation> ();
		AtomicUndoOperation currentAtomicOperation = null;
			
		public bool CanUndo {
			get {
				return this.undoStack.Count > 0;
			}
		}
		
		public void Undo ()
		{
			if (undoStack.Count <= 0)
				return;
			isInUndo = true;
			UndoOperation chunk = undoStack.Pop ();
			redoStack.Push (chunk);
			chunk.Undo (this);
			isInUndo = false;
			this.RequestUpdate (new UpdateAll ());
			this.CommitDocumentUpdate ();
		}
				
		public bool CanRedo {
			get {
				return this.redoStack.Count > 0;
			}
		}
		
		public void Redo ()
		{
			if (redoStack.Count <= 0)
				return;
			isInUndo = true;
			UndoOperation chunk = redoStack.Pop ();
			undoStack.Push (chunk);
			chunk.Redo (this);
			isInUndo = false;
			this.RequestUpdate (new UpdateAll ());
			this.CommitDocumentUpdate ();
		}
		
		public void BeginAtomicUndo ()
		{
			if (currentAtomicOperation == null)
			    currentAtomicOperation = new AtomicUndoOperation ();
		}
		public void EndAtomicUndo ()
		{
			if (currentAtomicOperation != null) {
				undoStack.Push (currentAtomicOperation);
				currentAtomicOperation = null;
			}
		}
		#endregion
		
		#region Folding
		List<FoldSegment> foldSegments = new List<FoldSegment> ();
		public bool HasFoldSegments {
			get {
				return foldSegments.Count != 0;
			}
		}
		
		public ReadOnlyCollection<FoldSegment> FoldSegments {
			get {
				return foldSegments.AsReadOnly ();
			}
		}
		
		public void UpdateFoldSegments (List<FoldSegment> newSegments)
		{
			if (newSegments == null) {
				return;
			}
			newSegments.Sort ();
			foreach (FoldSegment foldSegment in newSegments) {
				LineSegment startLine = splitter.GetLineByOffset (foldSegment.Offset);
				LineSegment endLine   = splitter.GetLineByOffset (foldSegment.EndOffset);
				foldSegment.EndColumn = foldSegment.EndOffset - endLine.Offset; 
				foldSegment.Column    = foldSegment.Offset - startLine.Offset; 
				foldSegment.EndLine   = endLine;
				foldSegment.StartLine = startLine;
			}
			int i = 0, j = 0;
			while (i < foldSegments.Count && j < newSegments.Count) {
				int cmp = foldSegments[i].CompareTo (newSegments [j]);
				if (cmp == 0) {
					newSegments[j].IsFolded = foldSegments[i].IsFolded;
					i++;j++;
				} else  if (cmp > 0) {
					j++;
				} else {
					i++;
				}
			}
			if (i < foldSegments.Count)
				newSegments.AddRange (foldSegments.GetRange (i, foldSegments.Count - i));
			bool needsUpdate = foldSegments.Count != newSegments.Count;
			foldSegments = newSegments;
			if (needsUpdate) {
				RequestUpdate (new UpdateAll ());
				CommitDocumentUpdate ();
			}
		}
		
		public List<FoldSegment> GetFoldingsFromOffset (int offset)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			foreach (FoldSegment foldSegment in foldSegments) {
				if (foldSegment.StartLine.Offset + foldSegment.Column < offset && offset < foldSegment.EndLine.Offset + foldSegment.EndColumn)
					result.Add (foldSegment);
			}
			return result;
		}
		
		public List<FoldSegment> GetFoldingContaining (LineSegment line)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			if (line != null) { 
				foreach (FoldSegment foldSegment in foldSegments) {
					if (foldSegment.StartLine != line && foldSegment.EndLine != line &&
					    foldSegment.StartLine.Offset < line.Offset && line.Offset < foldSegment.EndLine.Offset)
						result.Add (foldSegment);
				}
			}
			return result;
		}
		
		public List<FoldSegment> GetStartFoldings (LineSegment line)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			if (line != null) { 
				foreach (FoldSegment foldSegment in foldSegments) {
					if (foldSegment.StartLine == line)
						result.Add (foldSegment);
				}
			}
			return result;
		}
		
		public List<FoldSegment> GetEndFoldings (LineSegment line)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			if (line != null) { 
				foreach (FoldSegment foldSegment in foldSegments) {
					if (foldSegment.EndLine == line)
						result.Add (foldSegment);
				}
			}
			return result;
		}
		
		public List<FoldSegment> GetFoldingsBefore (int lineNumber)
		{
			return GetFoldingsBefore (GetLine (lineNumber));
		}
		
		public List<FoldSegment> GetFoldingsBefore (LineSegment line)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			if (line != null) {
				foreach (FoldSegment foldSegment in foldSegments) {
					if (foldSegment.StartLine.Offset < line.Offset)
						result.Add (foldSegment);
				}
			}
			return result;
		}
		
		public int GetLineCount (FoldSegment segment)
		{
			int result = 0;
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = segment.StartLine.Iter;
			while (iter.Current != segment.EndLine && iter.MoveNext ()) {
				result++;
			}
			return result;
		}
		#endregion
		
		public int VisualToLogicalLine (int visualLineNumber)
		{
			int result = visualLineNumber;
			int lastFoldingEnd = 0;
			LineSegment line = GetLine (result);
			foreach (FoldSegment foldSegment in foldSegments) {
				if (foldSegment.Offset > line.Offset)
					break;
				if (foldSegment.IsFolded && foldSegment.StartLine.Offset < line.Offset && lastFoldingEnd < foldSegment.EndOffset ) {
					result += GetLineCount (foldSegment);
					lastFoldingEnd = foldSegment.EndOffset;
					line = GetLine (result);
				}
			}
			return result;
		}
		
		public DocumentLocation LogicalToVisualLocation (DocumentLocation location)
		{
			int line = LogicalToVisualLine (location.Line);
			int column = 0;
			LineSegment lineSegment = this.GetLine (location.Line);
			return new DocumentLocation (line, column);
		}
		
		public int LogicalToVisualLine (int logicalLineNumber)
		{
			List<FoldSegment> foldings = GetFoldingsBefore (logicalLineNumber);
			int lastFoldingEnd = 0;
			int result = logicalLineNumber;
			foreach (FoldSegment folding in foldings) {
				if (folding.IsFolded && lastFoldingEnd < folding.EndOffset) {
					result -= GetLineCount (folding);
					lastFoldingEnd = folding.EndOffset;
				}
			}
			return result;
		}
		
		#region Update logic
		List<DocumentUpdateRequest> updateRequests = new List<DocumentUpdateRequest> ();
		
		public ReadOnlyCollection<DocumentUpdateRequest> UpdateRequests {
			get {
				return updateRequests.AsReadOnly ();
			}
		}

		public void RequestUpdate (DocumentUpdateRequest request)
		{
			//System.Console.WriteLine(request);
			updateRequests.Add (request);
		}
		
		public void CommitDocumentUpdate ()
		{
			if (DocumentUpdated != null)
				DocumentUpdated (this, EventArgs.Empty);
			updateRequests.Clear ();
		}
		
		public void CommitLineToEndUpdate (int line)
		{
			RequestUpdate (new LineToEndUpdate (line));
			CommitDocumentUpdate ();
		}
				
		public void CommitLineUpdate (int line)
		{
			RequestUpdate (new LineUpdate (line));
			CommitDocumentUpdate ();
		}
					
		public void CommitUpdateAll ()
		{
			RequestUpdate (new UpdateAll ());
			CommitDocumentUpdate ();
		}
						
		public void CommitMultipleLineUpdate (int start, int end)
		{
			RequestUpdate (new MultipleLineUpdate (start, end));
			CommitDocumentUpdate ();
		}
		
		public event EventHandler DocumentUpdated;
		#endregion
	}
}
