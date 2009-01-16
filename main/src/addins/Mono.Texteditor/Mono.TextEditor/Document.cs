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
using System.Linq;

namespace Mono.TextEditor
{
	public class Document : AbstractBuffer, IDisposable
	{
		IBuffer      buffer;
		LineSplitter splitter;
		SyntaxMode   syntaxMode = null;
		string       eol = null;
		
		string mimeType;
		string fileName;
		bool   readOnly;
		ReadOnlyCheckDelegate readOnlyCheckDelegate;
		
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
				mimeType = value;
				if (this.SyntaxMode == null)
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
				UpdateHighlighting ();
			}
		}
		
		public Document()
		{
			buffer   = new GapBuffer ();
			splitter = new LineSplitter (buffer);
			
			splitter.LineSegmentTree.LineChanged += delegate (object sender, LineEventArgs args) {
				if (LineChanged != null) 
					LineChanged (this, args);
			};
		/*	splitter.LineSegmentTree.LineInserted += delegate (object sender, LineEventArgs args) {
				if (LineInserted != null) 
					LineInserted (this, args);
			};*/
		}
		
		public event EventHandler<LineEventArgs> LineChanged;
	//	public event EventHandler<LineEventArgs> LineInserted;
		
		
		public override void Dispose ()
		{
			buffer = buffer.Kill ();
			splitter = splitter.Kill ();
			if (undoStack != null) {
				undoStack.Clear ();
				undoStack = null;
			}
			if (redoStack != null) {
				redoStack.Clear ();
				redoStack = null;
			}
			currentAtomicOperation = null;
			
			if (foldSegments != null) {
				foldSegments.Clear ();
				foldSegments = null;
			}
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
				ReplaceEventArgs args = new ReplaceEventArgs (0, oldLength, value);
				this.OnTextReplacing (args);
				this.buffer.Text = value;
				splitter.TextReplaced (this, args);
				UpdateHighlighting ();
				this.OnTextReplaced (args);
			}
		}
		
		void UpdateHighlighting ()
		{
			if (this.syntaxMode != null) {
				Mono.TextEditor.Highlighting.SyntaxModeService.StartUpdate (this, this.syntaxMode, 0, buffer.Length);
			//	Mono.TextEditor.Highlighting.SyntaxModeService.WaitForUpdate ();
			}
		}
		
		public System.IO.TextReader OpenTextReader ()
		{
			return new BufferedTextReader (this.buffer);
		}
		
		public override void Replace (int offset, int count, string value)
		{
			InterruptFoldWorker ();
//			Mono.TextEditor.Highlighting.SyntaxModeService.WaitForUpdate (true);
//			Debug.Assert (count >= 0);
//			Debug.Assert (0 <= offset && offset + count <= Length);
			int oldLineCount = this.LineCount;
			ReplaceEventArgs args = new ReplaceEventArgs (offset, count, value);
			OnTextReplacing (args);
/* insert/repla
			lock (syncObject) {
				int endOffset = offset + count;
				foldSegments = new List<FoldSegment> (foldSegments.Where (s => (s.Offset < offset || s.Offset >= endOffset) && 
				                                                               (s.EndOffset <= offset || s.EndOffset >= endOffset)));
			}*/
			if (!isInUndo) {
				UndoOperation operation = new UndoOperation (args, GetTextAt (offset, count));
				if (currentAtomicOperation != null) {
					currentAtomicOperation.Add (operation);
				} else {
					OnBeginUndo ();
					undoStack.Push (operation);
					OnEndUndo (operation);
				}
				foreach (UndoOperation redoOp in redoStack) {
					redoOp.Dispose ();
				}
				redoStack.Clear ();
			}
			
			buffer.Replace (offset, count, value);
			OnTextReplaced (args);
			splitter.TextReplaced (this, args);
			
			if (this.syntaxMode != null)
				Mono.TextEditor.Highlighting.SyntaxModeService.StartUpdate (this, this.syntaxMode, offset, value != null ? offset + value.Length : offset + count);
			if (oldLineCount != LineCount)
				this.CommitLineToEndUpdate (this.OffsetToLocation (offset).Line);
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
			return System.Math.Min (Length, line.Offset + System.Math.Min (line.EditableLength, location.Column));
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
		public class UndoOperation : IDisposable
		{
			ReplaceEventArgs args;
			string text;
			public virtual string Text {
				get {
					return text;
				}
			}
			public virtual ReplaceEventArgs Args {
				get {
					return args;
				}
			}
			protected UndoOperation()
			{
			}
			
			public UndoOperation (ReplaceEventArgs args, string text)
			{
				this.args = args;
				this.text = text;
			}
			
			public virtual void Dispose ()
			{
				args = null;
				if (Disposed != null) 
					Disposed (this, EventArgs.Empty);
			}
			
			public virtual void Undo (Document doc)
			{
				if (args.Value != null && args.Value.Length > 0)
					doc.Remove (args.Offset, args.Value.Length);
				if (!String.IsNullOrEmpty (text))
					doc.Insert (args.Offset, text);
				OnUndoDone ();
			}
			
			public virtual void Redo (Document doc)
			{
				doc.Replace (args.Offset, args.Count, args.Value);
				OnRedoDone ();
			}
			
			protected virtual void OnUndoDone ()
			{
				if (UndoDone != null)
					UndoDone (this, EventArgs.Empty);
			}
			public event EventHandler UndoDone;
			
			protected virtual void OnRedoDone ()
			{
				if (RedoDone != null)
					RedoDone (this, EventArgs.Empty);
			}
			public event EventHandler RedoDone;
			
			public event EventHandler Disposed;
		}
		
		class AtomicUndoOperation : UndoOperation
		{
			protected List<UndoOperation> operations = new List<UndoOperation> ();
			
			public List<UndoOperation> Operations {
				get {
					return operations;
				}
			}
			
			public override string Text {
				get {
					return null;
				}
			}
			public override ReplaceEventArgs Args {
				get {
					return null;
				}
			}
			
			public override void Dispose ()
			{
				if (operations != null) {
					foreach (UndoOperation operation in operations) {
						operation.Dispose ();
					}
					operations = null;
				}
				base.Dispose ();
			}
			
			public void Add (UndoOperation operation)
			{
				operations.Add (operation);
			}
			
			public override void Undo (Document doc)
			{
				for (int i = operations.Count - 1; i >= 0; i--) {
					operations[i].Undo (doc);
				}
				OnUndoDone ();
			}
			
			public override void Redo (Document doc)
			{
				foreach (UndoOperation operation in this.operations) {
					operation.Redo (doc);
				}
				OnRedoDone ();
			}
		}
		
		class KeyboardStackUndo : AtomicUndoOperation
		{
			bool isClosed = false;
			
			public bool IsClosed {
				get {
					return isClosed;
				}
				set {
					isClosed = value;
				}
			}
			
			public override string Text {
				get {
					return operations.Count > 0 ? operations [operations.Count - 1].Text : "";
				}
			}
			
			public override ReplaceEventArgs Args {
				get {
					return operations.Count > 0 ? operations [operations.Count - 1].Args : null;
				}
			}
		}
		
		bool isInUndo = false;
		Stack<UndoOperation> undoStack = new Stack<UndoOperation> ();
		Stack<UndoOperation> redoStack = new Stack<UndoOperation> ();
		AtomicUndoOperation currentAtomicOperation = null;
			
		public bool CanUndo {
			get {
				return this.undoStack.Count > 0 || currentAtomicOperation != null;
			}
		}
		
		UndoOperation[] savePoint = null;
		public bool IsDirty {
			get {
				if (this.currentAtomicOperation != null)
					return true;
				if (savePoint == null)
					return CanUndo;
				if (undoStack.Count != savePoint.Length) 
					return true;
				UndoOperation[] currentStack = undoStack.ToArray ();
				for (int i = 0; i < currentStack.Length; i++) {
					if (savePoint[i] != currentStack[i])
						return true;
				}
				return false;
			}
		}
		
		/// <summary>
		/// Marks the document not dirty at this point (should be called after save).
		/// </summary>
		public void SetNotDirtyState ()
		{
			OptimizeTypedUndo ();
			if (undoStack.Count > 0 && undoStack.Peek () is KeyboardStackUndo)
				((KeyboardStackUndo)undoStack.Peek ()).IsClosed = true;
			savePoint = undoStack.ToArray ();
		}
		
		public void OptimizeTypedUndo ()
		{
			if (undoStack.Count == 0)
				return;
			UndoOperation top = undoStack.Pop ();
			if (top.Args == null || top.Args.Value == null || top.Args.Value.Length != 1 || (top is KeyboardStackUndo && ((KeyboardStackUndo)top).IsClosed)) {
				undoStack.Push (top);
				return;
			}
			if (undoStack.Count == 0 || !(undoStack.Peek () is KeyboardStackUndo)) 
				undoStack.Push (new KeyboardStackUndo ());
			KeyboardStackUndo keyUndo = (KeyboardStackUndo)undoStack.Pop ();
			if (keyUndo.IsClosed) {
				undoStack.Push (keyUndo);
				keyUndo = new KeyboardStackUndo ();
			}
			if (keyUndo.Args != null && keyUndo.Args.Offset + 1 != top.Args.Offset) {
				keyUndo.IsClosed = true;
				undoStack.Push (keyUndo);
				keyUndo = new KeyboardStackUndo ();
			}
			keyUndo.Add (top);
			undoStack.Push (keyUndo);
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
		int atomicUndoLevel;
		public void BeginAtomicUndo ()
		{
			if (currentAtomicOperation == null) {
				Debug.Assert (atomicUndoLevel == 0); 
				currentAtomicOperation = new AtomicUndoOperation ();
				OnBeginUndo ();
			}
			atomicUndoLevel++;
		}
		
		public void EndAtomicUndo ()
		{
			atomicUndoLevel--;
			Debug.Assert (atomicUndoLevel >= 0); 
			
			if (atomicUndoLevel == 0 && currentAtomicOperation != null) {
				if (currentAtomicOperation.Operations.Count > 1) {
					undoStack.Push (currentAtomicOperation);
					OnEndUndo (currentAtomicOperation);
				} else {
					if (currentAtomicOperation.Operations.Count > 0) {
						undoStack.Push (currentAtomicOperation.Operations [0]);
						OnEndUndo (currentAtomicOperation.Operations [0]);
					} else {
						OnEndUndo (null);
					}
				}
				currentAtomicOperation = null;
			}
		}
		
		protected virtual void OnBeginUndo ()
		{
			if (BeginUndo != null) 
				BeginUndo (this, EventArgs.Empty);
		}
		
		protected virtual void OnEndUndo (UndoOperation undo)
		{
			if (EndUndo != null) 
				EndUndo (this, undo);
		}
		
		public delegate void UndoOperationHandler (object sender, UndoOperation operation);
		
		public event EventHandler         BeginUndo;
		public event UndoOperationHandler EndUndo;
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
		
		class FoldSegmentWorkerThread : WorkerThread
		{
			Document doc;
			List<FoldSegment> newSegments;
			
			public FoldSegmentWorkerThread (Document doc, List<FoldSegment> newSegments)
			{
				this.doc = doc;
				this.newSegments = newSegments;
			}
			
			protected override void InnerRun ()
			{
				newSegments.Sort ();
				foreach (FoldSegment foldSegment in newSegments) {
					if (IsStopping)
						return;
					LineSegment startLine = doc.splitter.GetLineByOffset (foldSegment.Offset);
					LineSegment endLine   = doc.splitter.GetLineByOffset (foldSegment.EndOffset);
					foldSegment.EndColumn = foldSegment.EndOffset - endLine.Offset; 
					foldSegment.Column    = foldSegment.Offset - startLine.Offset; 
					foldSegment.EndLine   = endLine;
					foldSegment.StartLine = startLine;
				}
				int i = 0, j = 0;
				while (i < doc.foldSegments.Count && j < newSegments.Count) {
					if (IsStopping)
						return;
					int cmp = doc.foldSegments[i].CompareTo (newSegments [j]);
					if (cmp == 0) {
						if (newSegments[j].Length == doc.foldSegments[i].Length) 
							newSegments[j].IsFolded = doc.foldSegments[i].IsFolded;
						i++;j++;
					} else  if (cmp > 0) {
						j++;
					} else {
						i++;
					}
				}
				if (i < doc.foldSegments.Count)
					newSegments.AddRange (doc.foldSegments.GetRange (i, doc.foldSegments.Count - i));
				GLib.Timeout.Add (0, delegate {
//					bool needsUpdate = doc.foldSegments.Count != newSegments.Count;
					doc.foldSegments = newSegments;
//					if (needsUpdate) {
						doc.RequestUpdate (new UpdateAll ());
						doc.CommitDocumentUpdate ();
//					}
					return false;
				});
				base.Stop ();
			}
		}
		
		readonly object syncObject = new object();
		FoldSegmentWorkerThread foldSegmentWorkerThread = null;
		
		public void UpdateFoldSegments (List<FoldSegment> newSegments)
		{
			if (newSegments == null) {
				return;
			}
			
			lock (syncObject) {
				if (foldSegmentWorkerThread != null) 
					foldSegmentWorkerThread.Stop ();
				
				foldSegmentWorkerThread = new FoldSegmentWorkerThread (this, newSegments);
				foldSegmentWorkerThread.Start ();
			}
		}
		
		void InterruptFoldWorker ()
		{
			lock (syncObject) {
				if (foldSegmentWorkerThread != null) {
					if (!foldSegmentWorkerThread.IsStopping) {
						foldSegmentWorkerThread.Stop ();
					}
					foldSegmentWorkerThread.WaitForFinish ();
					foldSegmentWorkerThread = null;
				}
			}
		}
		
		public void ClearFoldSegments ()
		{
			lock (syncObject) {
				if (foldSegmentWorkerThread != null) 
					foldSegmentWorkerThread.Stop ();
				foldSegments.Clear ();
			}
		}
		delegate int Comparer (int idx);
		
		int BinarySearchIndex (Comparer cmp)
		{
			int low = 0;
			int high = foldSegments.Count - 1;
			while (low <= high) {
				int mid = (low + high) / 2;
				int c = cmp (mid);
				if (c > 0) {
					high = mid - 1;
				} else if (c < 0) {
					low = mid + 1;
				} else {
					if (mid == 0 || cmp (mid - 1) != 0)
						return mid;
					high = mid - 1;
				}
			}
			return -1;
		}
		
		IEnumerable<FoldSegment> GatherFoldings (Comparer cmp)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			int startIndex = BinarySearchIndex (cmp);
			if (startIndex >= 0) {
				for (int i = startIndex; i < foldSegments.Count; i++) {
					if (cmp(i) == 0)
						result.Add (foldSegments[i]);
					break;
				}
			}
			return result;
		}
		
		public IEnumerable<FoldSegment> GetFoldingsFromOffset (int offset)
		{
			return GatherFoldings (delegate (int i) {
				if (foldSegments[i].StartLine.Offset + foldSegments[i].Column >= offset)
					return 1;
				if (foldSegments[i].EndLine.Offset + foldSegments[i].EndColumn <= offset)
					return -1;
				return 0;
			});
			
			//return foldSegments.Where (s => s.StartLine.Offset + s.Column < offset && offset < s.EndLine.Offset + s.EndColumn);
		}
		
		public IEnumerable<FoldSegment> GetFoldingContaining (int lineNumber)
		{
			return GetFoldingContaining (this.GetLine (lineNumber));
		}
		
		public IEnumerable<FoldSegment> GetFoldingContaining (LineSegment line)
		{
			if (line == null)
				return new FoldSegment[0];
			return GatherFoldings (delegate (int i) {
				if (foldSegments[i].StartLine.Offset >= line.Offset)
					return 1;
				if (foldSegments[i].EndLine.Offset <= line.Offset)
					return -1;
				return 0;
			});
		}
		
		public IEnumerable<FoldSegment> GetStartFoldings (int lineNumber)
		{
			return GetStartFoldings (this.GetLine (lineNumber));
		}
		
		public IEnumerable<FoldSegment> GetStartFoldings (LineSegment line)
		{
			if (line == null)
				return new FoldSegment[0];
			return GatherFoldings (delegate (int i) {
				if (foldSegments[i].StartLine.Offset > line.Offset)
					return 1;
				if (foldSegments[i].StartLine.Offset < line.Offset)
					return -1;
				return 0;
			});
		}
		
		public IEnumerable<FoldSegment> GetEndFoldings (int lineNumber)
		{
			return GetEndFoldings (this.GetLine (lineNumber));
		}
		
		public IEnumerable<FoldSegment> GetEndFoldings (LineSegment line)
		{
			if (line == null)
				return new FoldSegment[0];
			List<FoldSegment> result = new List<FoldSegment> ();
			for (int i = 0; i < foldSegments.Count; i++) {
				if (foldSegments[i].StartLine.Offset > line.Offset)
					break;
				if (foldSegments[i].EndLine == line)
					result.Add (foldSegments[i]);
			}
			return result;
		}
		
		public IEnumerable<FoldSegment> GetFoldingsBefore (int lineNumber)
		{
			return GetFoldingsBefore (GetLine (lineNumber));
		}
		
		public IEnumerable<FoldSegment> GetFoldingsBefore (LineSegment line)
		{
			if (line == null)
				return new FoldSegment[0];
/*			int idx = BinarySearchIndex (delegate (int i) {
				if (foldSegments[i].StartLine.Offset > line.Offset)
					return 0;
				if (foldSegments[i].StartLine.Offset > line.Offset)
					return -1;
				return 1;
			});
			System.Console.WriteLine(idx);
			if (idx < 0) {
				if (foldSegments.Count > 0 && foldSegments[foldSegments.Count - 1].Offset < line.Offset)
					return FoldSegments;
				return new FoldSegment[0];
			}
			return foldSegments.GetRange (0, idx);
			*/
			List<FoldSegment> result = new List<FoldSegment> ();
			for (int i = 0; i < foldSegments.Count; i++) {
				if (foldSegments[i].StartLine.Offset > line.Offset)
					break;
				result.Add (foldSegments[i]);
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
		

		public void AddMarker (int lineNumber, TextMarker marker)
		{
			AddMarker (this.GetLine (lineNumber), marker);
		}
		public void AddMarker (LineSegment line, TextMarker marker)
		{
			line.AddMarker (marker);
			this.CommitLineUpdate (line);
		}
		
		public void RemoveMarker (int lineNumber, TextMarker marker)
		{
			RemoveMarker (this.GetLine (lineNumber), marker);
		}
		public void RemoveMarker (LineSegment line, TextMarker marker)
		{
			line.RemoveMarker (marker);
			this.CommitLineUpdate (line);
		}
		public void RemoveMarker (int lineNumber, Type type)
		{
			RemoveMarker (this.GetLine (lineNumber), type);
		}
		public void RemoveMarker (LineSegment line, Type type)
		{
			line.RemoveMarker (type);
			this.CommitLineUpdate (line);
		}
		
		public bool Contains (int offset)
		{
			return new Segment (0, Length).Contains (offset);
		}
		
		public bool Contains (ISegment segment)
		{
			return new Segment (0, Length).Contains (segment);
		}
		
		public int VisualToLogicalLine (int visualLineNumber)
		{
			if (visualLineNumber <= 0)
				return 0;
			if (visualLineNumber >= LineCount)
				return visualLineNumber;
			int result = visualLineNumber;
			int lastFoldingEnd = 0;
			LineSegment line = GetLine (result);
			for (int i = 0; i < foldSegments.Count; i++) {
				FoldSegment foldSegment = foldSegments[i];
				if (foldSegment.Offset > line.Offset)
					break;
				if (foldSegment.IsFolded && foldSegment.StartLine.Offset < line.Offset && lastFoldingEnd < foldSegment.EndOffset ) {
					result += GetLineCount (foldSegment);
					lastFoldingEnd = foldSegment.EndOffset;
					line = GetLine (result);
					if (line == null)
						return result;
				}
			}
			return result;
		}
		
		public DocumentLocation LogicalToVisualLocation (TextEditorData editor, DocumentLocation location)
		{
			int line = LogicalToVisualLine (location.Line);
			LineSegment lineSegment = this.GetLine (location.Line);
			int column = lineSegment != null ? lineSegment.GetVisualColumn (editor, this, location.Column) : location.Column;
			return new DocumentLocation (line, column);
		}
		
		public int LogicalToVisualLine (int logicalLineNumber)
		{
			int lastFoldingEnd = 0;
			int result = logicalLineNumber;
			foreach (FoldSegment folding in GetFoldingsBefore (logicalLineNumber)) {
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
		// Use CanEdit (int lineNumber) instead for getting a request
		// if a part of a document can be read. ReadOnly should generally not be used
		// for deciding, if a document is readonly or not.
		public bool ReadOnly {
			get {
				return readOnly;
			}
			set {
				readOnly = value;
			}
		}
		
		public ReadOnlyCheckDelegate ReadOnlyCheckDelegate {
			get { return readOnlyCheckDelegate; }
			set { readOnlyCheckDelegate = value; }
		}


		public void RequestUpdate (DocumentUpdateRequest request)
		{
			lock (syncObject) {
				updateRequests.Add (request);
			}
		}
		
		public void CommitDocumentUpdate ()
		{
			lock (syncObject) {
				if (DocumentUpdated != null)
					DocumentUpdated (this, EventArgs.Empty);
				updateRequests.Clear ();
			}
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
		
		public void CommitLineUpdate (LineSegment line)
		{
			CommitLineUpdate (this.OffsetToLineNumber (line.Offset));
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
	
		#region Helper functions
		public const string openBrackets    = "([{<";
		public const string closingBrackets = ")]}>";
		
		public static bool IsBracket (char ch)
		{
			return (openBrackets + closingBrackets).IndexOf (ch) >= 0;
		}
		
		public static bool IsWordSeparator (char ch)
		{
			return Char.IsWhiteSpace (ch) || (Char.IsPunctuation (ch) && ch != '_');
		}
		
		public bool IsWholeWordAt (int offset, int length)
		{
			return (offset == 0 || IsWordSeparator (GetCharAt (offset - 1))) &&
				   (offset + length == Length || IsWordSeparator (GetCharAt (offset + length)));
		}
		
		public int GetMatchingBracketOffset (int offset)
		{
			if (offset < 0 || offset >= Length)
				return -1;
			char ch = GetCharAt (offset);
			int bracket = openBrackets.IndexOf (ch);
			int result;
			if (bracket >= 0) {
				result = SearchMatchingBracketForward (offset + 1, bracket);
			} else {
				bracket = closingBrackets.IndexOf (ch);
				if (bracket >= 0) {
					result = SearchMatchingBracketBackward (offset - 1, bracket);
				} else {
					result = -1;
				}
			}
			return result;
		}
		
		int SearchMatchingBracketForward (int offset, int bracket)
		{
			return SearchMatchingBracket (offset, closingBrackets[bracket], openBrackets[bracket], 1);
		}
		
		int SearchMatchingBracketBackward (int offset, int bracket)
		{
			return SearchMatchingBracket (offset, openBrackets[bracket], closingBrackets[bracket], -1);
		}
		
		int SearchMatchingBracket (int offset, char openBracket, char closingBracket, int direction)
		{
			bool isInString       = false;
			bool isInChar         = false;	
			bool isInBlockComment = false;
			int depth = -1;
			while (offset >= 0 && offset < Length) {
				char ch = GetCharAt (offset);
				switch (ch) {
					case '/':
						if (isInBlockComment) 
							isInBlockComment = GetCharAt (offset + direction) != '*';
						if (!isInString && !isInChar && offset - direction < Length) 
							isInBlockComment = offset > 0 && GetCharAt (offset - direction) == '*';
						break;
					case '"':
						if (!isInChar && !isInBlockComment) 
							isInString = !isInString;
						break;
					case '\'':
						if (!isInString && !isInBlockComment) 
							isInChar = !isInChar;
						break;
					default :
						if (ch == closingBracket) {
							if (!(isInString || isInChar || isInBlockComment)) 
								--depth;
						} else if (ch == openBracket) {
							if (!(isInString || isInChar || isInBlockComment)) {
								++depth;
								if (depth == 0) 
									return offset;
							}
						}
						break;
				}
				offset += direction;
			}
			return -1;
		}
		
		public enum CharacterClass {
			Unknown,
			Whitespace,
			IdentifierPart
		}
		
		public static CharacterClass GetCharacterClass (char ch)
		{
			if (Char.IsWhiteSpace (ch))
				return CharacterClass.Whitespace;
			if (Char.IsLetterOrDigit (ch) || ch == '_')
				return CharacterClass.IdentifierPart;
			return CharacterClass.Unknown;
		}
		
		#endregion
	}
	
	public delegate bool ReadOnlyCheckDelegate (int line);
}
