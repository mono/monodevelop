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
using System.Diagnostics;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.Utils;
using System.Linq;
using System.ComponentModel;

namespace Mono.TextEditor
{
	public class Document : IBuffer
	{
		IBuffer      buffer;
		ILineSplitter splitter;
		SyntaxMode   syntaxMode = null;
		
		string mimeType;
		
		bool   readOnly;
		ReadOnlyCheckDelegate readOnlyCheckDelegate;
		
		public string MimeType {
			get {
				return mimeType;
			}
			set {
				if (mimeType != value) {
					mimeType = value;
					this.SyntaxMode = SyntaxModeService.GetSyntaxMode (value);
				}
			}
		}
		
		public string FileName {
			get;
			set;
		}	
		
		public bool HeightChanged {
			get;
			set;
		}
		
		public SyntaxMode SyntaxMode {
			get {
				return syntaxMode ?? new SyntaxMode ();
			}
			set {
				syntaxMode = value;
				UpdateHighlighting ();
			}
		}
		
		public object Tag {
			get;
			set;
		}
		
		protected Document (IBuffer buffer,ILineSplitter splitter)
		{
			this.buffer = buffer;
			this.splitter = splitter;
			splitter.LineChanged += SplitterLineSegmentTreeLineChanged;
			splitter.LineRemoved += HandleSplitterLineSegmentTreeLineRemoved;
			foldSegmentTree.InstallListener (this);
			foldSegmentTree.tree.NodeRemoved += delegate(object sender, RedBlackTree<FoldSegment>.RedBlackTreeNodeEventArgs e) {
				if (e.Node.IsFolded)
					foldedSegments.Remove (e.Node);
			};
		}

		public Document () : this(new GapBuffer (), new LineSplitter ())
		{
		}
		
		public Document (string text) : this()
		{
			Text = text;
		}

		public static Document CreateImmutableDocument (string text)
		{
			return new Document(new StringBuffer(text), new PrimitiveLineSplitter()) {
				SuppressHighlightUpdate = true,
				Text = text,
				ReadOnly = true
			};
		}
		
		~Document ()
		{
			if (foldSegmentWorker != null) {
				foldSegmentWorker.Dispose ();
				foldSegmentWorker = null;
			}
		}

		void SplitterLineSegmentTreeLineChanged (object sender, LineEventArgs e)
		{
			if (LineChanged != null)
				LineChanged (this, e);
		}
		
		public event EventHandler<LineEventArgs> LineChanged;
	//	public event EventHandler<LineEventArgs> LineInserted;
		
		#region Buffer implementation
		public int Length {
			get {
				return this.buffer.Length;
			}
		}

		public bool SuppressHighlightUpdate { get; set; }
		
		public string Text {
			get {
				return this.buffer.Text;
			}
			set {
				if (!SuppressHighlightUpdate)
					Mono.TextEditor.Highlighting.SyntaxModeService.WaitUpdate (this);
				ReplaceEventArgs args = new ReplaceEventArgs (0, Length, value);
				this.OnTextReplacing (args);
				this.buffer.Text = value;
				splitter.Initalize (value);
				UpdateHighlighting ();
				this.OnTextReplaced (args);
				this.OnTextSet (EventArgs.Empty);
				this.CommitUpdateAll ();
				this.ClearUndoBuffer ();
			}
		}
		
		public void UpdateHighlighting ()
		{
			if (this.syntaxMode != null && !SuppressHighlightUpdate)
				Mono.TextEditor.Highlighting.SyntaxModeService.StartUpdate (this, this.syntaxMode, 0, buffer.Length);
		}
		
		public System.IO.TextReader OpenTextReader ()
		{
			return new BufferedTextReader (this.buffer);
		}
		
		void IBuffer.Insert (int offset, string text)
		{
			((IBuffer)this).Replace (offset, 0, text);
		}
		
		void IBuffer.Remove (int offset, int count)
		{
			((IBuffer)this).Replace (offset, count, null);
		}
		
		void IBuffer.Remove (ISegment segment)
		{
			((IBuffer)this).Remove (segment.Offset, segment.Length);
		}

		void IBuffer.Replace (int offset, int count, string value)
		{
			if (atomicUndoLevel == 0) {
				if (this.syntaxMode != null && !SuppressHighlightUpdate)
					Mono.TextEditor.Highlighting.SyntaxModeService.WaitUpdate (this);
			}
			InterruptFoldWorker ();
			//			Mono.TextEditor.Highlighting.SyntaxModeService.WaitForUpdate (true);
		//			Debug.Assert (count >= 0);
		//			Debug.Assert (0 <= offset && offset + count <= Length);
			int oldLineCount = this.LineCount;
			ReplaceEventArgs args = new ReplaceEventArgs (offset, count, value);
			if (Partitioner != null)
				Partitioner.TextReplacing (args);
			OnTextReplacing (args);
			value = args.Value;
			/* insert/repla
			lock (syncObject) {
				int endOffset = offset + count;
				foldSegments = new List<FoldSegment> (foldSegments.Where (s => (s.Offset < offset || s.Offset >= endOffset) && 
				                                                               (s.EndOffset <= offset || s.EndOffset >= endOffset)));
			}*/	
			UndoOperation operation = null;
			if (!isInUndo) {
				operation = new UndoOperation (args, count > 0 ? GetTextAt (offset, count) : "");
				if (currentAtomicOperation != null) {
					currentAtomicOperation.Add (operation);
				} else {
					OnBeginUndo ();
					undoStack.Push (operation);
					OnEndUndo (new UndoOperationEventArgs (operation));
				}
				redoStack.Clear ();
			}
			
			buffer.Replace (offset, count, value);
			splitter.TextReplaced (this, args);
			if (Partitioner != null)
				Partitioner.TextReplaced (args);
			OnTextReplaced (args);
			
			UpdateUndoStackOnReplace (args);
			if (operation != null)
				operation.Setup (this, args);
			
			if (this.syntaxMode != null && !SuppressHighlightUpdate) {
				Mono.TextEditor.Highlighting.SyntaxModeService.StartUpdate (this, this.syntaxMode, offset, value != null ? offset + value.Length : offset + count);
			}
			if (oldLineCount != LineCount)
				this.CommitLineToEndUpdate (this.OffsetToLocation (offset).Line);
		}
		
		public string GetTextBetween (int startOffset, int endOffset)
		{
			if (startOffset < 0)
				throw new ArgumentException ("startOffset < 0");
			if (startOffset > Length)
				throw new ArgumentException ("startOffset > Length");
			if (endOffset < 0)
				throw new ArgumentException ("startOffset < 0");
			if (endOffset > Length)
				throw new ArgumentException ("endOffset > Length");
			
			return buffer.GetTextAt (startOffset, endOffset - startOffset);
		}
		
		public string GetTextBetween (DocumentLocation start, DocumentLocation end)
		{
			return GetTextBetween (LocationToOffset (start), LocationToOffset (end));
		}
		
		public string GetTextBetween (int startLine, int startColumn, int endLine, int endColumn)
		{
			return GetTextBetween (LocationToOffset (startLine, startColumn), LocationToOffset (endLine, endColumn));
		}
		
		
		public string GetTextAt (int offset, int count)
		{
			if (offset < 0)
				throw new ArgumentException ("startOffset < 0");
			if (offset > Length)
				throw new ArgumentException ("startOffset > Length");
			if (count < 0)
				throw new ArgumentException ("count < 0");
			if (offset + count > Length)
				throw new ArgumentException ("offset + count is beyond EOF");
			return buffer.GetTextAt (offset, count);
		}
		
		public string GetTextAt (ISegment segment)
		{
			return GetTextAt (segment.Offset, segment.Length);
		}
		
		/// <summary>
		/// Gets the line text without the delimiter.
		/// </summary>
		/// <returns>
		/// The line text.
		/// </returns>
		/// <param name='line'>
		/// The line number.
		/// </param>
		public string GetLineText (int line)
		{
			var lineSegment = GetLine (line);
			return lineSegment != null ? GetTextAt (lineSegment.Offset, lineSegment.EditableLength) : null;
		}
		
		public string GetLineText (int line, bool includeDelimiter)
		{
			var lineSegment = GetLine (line);
			return includeDelimiter ? GetTextAt (lineSegment) : GetTextAt (lineSegment.Offset, lineSegment.EditableLength);
		}
		
		public char GetCharAt (int offset)
		{
			return buffer.GetCharAt (offset);
		}
		
		public IEnumerable<int> SearchForward (string pattern, int startIndex)
		{
			return buffer.SearchForward (pattern, startIndex);
		}
		
		public IEnumerable<int> SearchForwardIgnoreCase (string pattern, int startIndex)
		{
			return SearchForwardIgnoreCase (pattern, startIndex);
		}
		
		public IEnumerable<int> SearchBackward (string pattern, int startIndex)
		{
			return SearchBackward (pattern, startIndex);
		}
		
		public IEnumerable<int> SearchBackwardIgnoreCase (string pattern, int startIndex)
		{
			return SearchBackwardIgnoreCase (pattern, startIndex);
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
		
		protected virtual void OnTextSet (EventArgs e)
		{
			EventHandler handler = this.TextSet;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler TextSet;
		#endregion
		
		#region Line Splitter operations
		public IEnumerable<LineSegment> Lines {
			get {
				return splitter.Lines;
			}
		}
		
		public int LineCount {
			get {
				return splitter.Count;
			}
		}

		public IEnumerable<LineSegment> GetLinesBetween (int startLine, int endLine)
		{
			return splitter.GetLinesBetween (startLine, endLine);
		}

		public IEnumerable<LineSegment> GetLinesStartingAt (int startLine)
		{
			return splitter.GetLinesStartingAt (startLine);
		}

		public IEnumerable<LineSegment> GetLinesReverseStartingAt (int startLine)
		{
			return splitter.GetLinesReverseStartingAt (startLine);
		}
		
		public int LocationToOffset (int line, int column)
		{
			return LocationToOffset (new DocumentLocation (line, column));
		}
		
		public int LocationToOffset (DocumentLocation location)
		{
//			if (location.Column < DocumentLocation.MinColumn)
//				throw new ArgumentException ("column < MinColumn");
			if (location.Line > this.splitter.Count || location.Line < DocumentLocation.MinLine)
				return -1;
			LineSegment line = GetLine (location.Line);
			return System.Math.Min (Length, line.Offset + System.Math.Max (0, System.Math.Min (line.EditableLength, location.Column - 1)));
		}
		
		public DocumentLocation OffsetToLocation (int offset)
		{
			int lineNr = splitter.OffsetToLineNumber (offset);
			if (lineNr < DocumentLocation.MinLine)
				return DocumentLocation.Empty;
			LineSegment line = GetLine (lineNr);
			return new DocumentLocation (lineNr, System.Math.Min (line.Length, offset - line.Offset) + 1);
		}

		public string GetLineIndent (int lineNumber)
		{
			return GetLineIndent (GetLine (lineNumber));
		}
		
		public string GetLineIndent (LineSegment segment)
		{
			if (segment == null)
				return "";
			return segment.GetIndentation (this);
		}
		
		public LineSegment GetLine (int lineNumber)
		{
			if (lineNumber < DocumentLocation.MinLine)
				return null;
			
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
		public class UndoOperation
		{
			ReplaceEventArgs args;
			string text;
			int startOffset, length;
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
			
			public object Tag {
				get;
				set;
			}
			
			protected UndoOperation()
			{
			}

			public virtual bool ChangedLine (LineSegment line)
			{
				if (Args == null || line == null)
					return false;
				return startOffset <= line.Offset - line.DelimiterLength && line.Offset <= startOffset + length 
						|| line.Offset - line.DelimiterLength <= startOffset && startOffset <= line.Offset + line.EditableLength
						;
					; //line.Contains (Args.Offset);
			}
			
			public UndoOperation (ReplaceEventArgs args, string text)
			{
				this.args = args;
				this.text = text;
			}
			
			static int GetDelta (ReplaceEventArgs args)
			{
				int result = -args.Count;
				if (!String.IsNullOrEmpty (args.Value))
					result += args.Value.Length;
				return result;
			}
			
			internal void Setup (Document doc, ReplaceEventArgs args)
			{
				if (args != null) {
					this.startOffset = args.Offset;
					if (!String.IsNullOrEmpty (args.Value))
						this.length  = args.Value.Length;
				}
			}
			
			internal virtual void InformTextReplace (ReplaceEventArgs args)
			{
				if (args.Offset < startOffset) {
					startOffset = System.Math.Max (startOffset + GetDelta(args), args.Offset);
				} else if (args.Offset < startOffset + length) {
					length = System.Math.Max (length + GetDelta(args), startOffset - args.Offset);
				}
			}
			
			public virtual void Undo (Document doc)
			{
				if (args.Value != null && args.Value.Length > 0)
					((IBuffer)doc).Remove (args.Offset, args.Value.Length);
				if (!String.IsNullOrEmpty (text))
					((IBuffer)doc).Insert (args.Offset, text);
				OnUndoDone ();
			}
			
			public virtual void Redo (Document doc)
			{
				((IBuffer)doc).Replace (args.Offset, args.Count, args.Value);
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
			
			
			internal override void InformTextReplace (ReplaceEventArgs args)
			{
				operations.ForEach (o => o.InformTextReplace (args));
			}
			
			public override bool ChangedLine (LineSegment line)
			{
				foreach (UndoOperation op in Operations) {
					if (op.ChangedLine (line))
						return true;
				}
				return false;
			}
			
			public void Insert (int index, UndoOperation operation)
			{
				operations.Insert (index, operation);
			}
			
			public void Add (UndoOperation operation)
			{
				operations.Add (operation);
			}
			
			public override void Undo (Document doc)
			{
				for (int i = operations.Count - 1; i >= 0; i--) {
					operations[i].Undo (doc);
					doc.OnUndone (new UndoOperationEventArgs (operations[i]));
				}
				OnUndoDone ();
			}
			
			public override void Redo (Document doc)
			{
				foreach (UndoOperation operation in this.operations) {
					operation.Redo (doc);
					doc.OnRedone (new UndoOperationEventArgs (operation));
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
		
		// The undo stack needs to be updated on replace, because the text editor
		// draws a replace operation marker at the left margin.
		public void UpdateUndoStackOnReplace (ReplaceEventArgs args)
		{
			foreach (UndoOperation op in undoStack) {
				op.InformTextReplace (args);
			}
			// since we're only displaying the undo stack it's not required to update the redo stack
//			foreach (UndoOperation op in redoStack) {
//				op.InformTextReplace (args);
//			}
		}
		
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
		
		public enum LineState {
			Unchanged,
			Dirty,
			Changed
		}
		
		public LineState GetLineState (int lineNumber)
		{
			LineSegment line = GetLine (lineNumber);
			foreach (UndoOperation op in undoStack) {
				if (op.ChangedLine (line)) {
					if (savePoint != null) {
						foreach (UndoOperation savedUndo in savePoint) {
							if (op == savedUndo)
								return LineState.Changed;
						}
					}
					return LineState.Dirty;
				}
			}
			return LineState.Unchanged;
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
			this.CommitUpdateAll ();
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
		
		public int GetCurrentUndoDepth ()
		{
			return undoStack.Count;
		}
		
		public void StackUndoToDepth (int depth)
		{
			if (undoStack.Count == depth)
				return;
			AtomicUndoOperation atomicUndo = new AtomicUndoOperation ();
			while (undoStack.Count > depth) {
				atomicUndo.Operations.Insert (0, undoStack.Pop ());
			}
			undoStack.Push (atomicUndo);
		}
		
		public void MergeUndoOperations (int number)
		{
			number = System.Math.Min (number, undoStack.Count);
			AtomicUndoOperation atomicUndo = new AtomicUndoOperation ();
			while (number-- > 0) {
				atomicUndo.Insert (0, undoStack.Pop ());
			}
			undoStack.Push (atomicUndo);
		}
		
		public void Undo ()
		{
			if (undoStack.Count <= 0)
				return;
			OnBeforeUndoOperation (EventArgs.Empty);
			isInUndo = true;
			UndoOperation operation = undoStack.Pop ();
			redoStack.Push (operation);
			operation.Undo (this);
			isInUndo = false;
			OnUndone (new UndoOperationEventArgs (operation));
			this.RequestUpdate (new UpdateAll ());
			this.CommitDocumentUpdate ();
		}
		
		internal protected virtual void OnUndone (UndoOperationEventArgs e)
		{
			EventHandler<UndoOperationEventArgs> handler = this.Undone;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<UndoOperationEventArgs> Undone;
		
		internal protected virtual void OnBeforeUndoOperation (EventArgs e)
		{
			var handler = this.BeforeUndoOperation;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler BeforeUndoOperation;

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
			UndoOperation operation = redoStack.Pop ();
			undoStack.Push (operation);
			operation.Redo (this);
			isInUndo = false;
			OnRedone (new UndoOperationEventArgs (operation));
			this.RequestUpdate (new UpdateAll ());
			this.CommitDocumentUpdate ();
		}
		
		internal protected virtual void OnRedone (UndoOperationEventArgs e)
		{
			EventHandler<UndoOperationEventArgs> handler = this.Redone;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler<UndoOperationEventArgs> Redone;
		
		int atomicUndoLevel;
		
		public bool IsInAtomicUndo {
			get {
				return atomicUndoLevel > 0;
			}
		}
		
		public void BeginAtomicUndo ()
		{
			if (atomicUndoLevel == 0) {
				if (this.syntaxMode != null && !SuppressHighlightUpdate)
					Mono.TextEditor.Highlighting.SyntaxModeService.WaitUpdate (this);
			}
			if (currentAtomicOperation == null) {
				Debug.Assert (atomicUndoLevel == 0); 
				currentAtomicOperation = new AtomicUndoOperation ();
				OnBeginUndo ();
			}
			atomicUndoLevel++;
		}
		
		public void EndAtomicUndo ()
		{
			if (atomicUndoLevel <= 0)
				throw new InvalidOperationException ("There is no atomic undo operation running.");
			atomicUndoLevel--;
			Debug.Assert (atomicUndoLevel >= 0); 
			
			if (atomicUndoLevel == 0 && currentAtomicOperation != null) {
				if (currentAtomicOperation.Operations.Count > 1) {
					undoStack.Push (currentAtomicOperation);
					OnEndUndo (new UndoOperationEventArgs (currentAtomicOperation));
				} else {
					if (currentAtomicOperation.Operations.Count > 0) {
						undoStack.Push (currentAtomicOperation.Operations [0]);
						OnEndUndo (new UndoOperationEventArgs (currentAtomicOperation.Operations [0]));
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
		
		public void ClearUndoBuffer ()
		{
			undoStack.Clear ();
			redoStack.Clear ();
		}
		
		[Serializable]
		public sealed class UndoOperationEventArgs : EventArgs
		{
			public UndoOperation Operation { get; private set; }
			
			public UndoOperationEventArgs (UndoOperation operation)
			{
				this.Operation = operation;
			}
			
		}
		
		protected virtual void OnEndUndo (UndoOperationEventArgs e)
		{
			EventHandler<UndoOperationEventArgs> handler = this.EndUndo;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler                         BeginUndo;
		public event EventHandler<UndoOperationEventArgs> EndUndo;
		#endregion
		
		#region Folding
		
		SegmentTree<FoldSegment> foldSegmentTree = new SegmentTree<FoldSegment> ();
		
		public bool IgnoreFoldings {
			get;
			set;
		}
		
		public bool HasFoldSegments {
			get {
				return FoldSegments.Any ();
			}
		}
		
		public IEnumerable<FoldSegment> FoldSegments {
			get {
				return foldSegmentTree.Segments;
			}
		}
		
		readonly object syncObject = new object();
		BackgroundWorker foldSegmentWorker = null;
		BackgroundWorker FoldSegmentWorker {
			get {
				if (foldSegmentWorker == null) {
					foldSegmentWorker = new BackgroundWorker ();
					foldSegmentWorker.WorkerSupportsCancellation = true;
					foldSegmentWorker.DoWork += UpdateFoldSegmentWorker;
				}
				return foldSegmentWorker;
			}
		}
		
		public void UpdateFoldSegments (List<FoldSegment> newSegments)
		{
			UpdateFoldSegments (newSegments, true);
		}
		
		public void UpdateFoldSegments (List<FoldSegment> newSegments, bool runInThread)
		{
			if (newSegments == null) {
				return;
			}
			
			InterruptFoldWorker ();
			if (!runInThread) {
				UpdateFoldSegmentWorker (null, new DoWorkEventArgs (newSegments));
				return;
			}
			FoldSegmentWorker.RunWorkerAsync (newSegments);
		}
		
		void RemoveFolding (FoldSegment folding)
		{
			folding.isAttached = false;
			if (folding.isFolded)
				foldedSegments.Remove (folding);
			foldedSegments.Remove (folding);
		}
		
		/// <summary>
		/// Updates the fold segments in a background worker thread. Don't call this method outside of a background worker.
		/// Use UpdateFoldSegments instead.
		/// </summary>
		public void UpdateFoldSegmentWorker (object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;
			var newSegments = (List<FoldSegment>)e.Argument;
			var oldSegments = new List<FoldSegment> (FoldSegments);
			int oldIndex = 0;
			newSegments.Sort ();
			var newFoldedSegments = new HashSet<FoldSegment> ();
			foreach (FoldSegment newFoldSegment in newSegments) {
				if (worker != null && worker.CancellationPending)
					return;
				int offset = newFoldSegment.Offset;
				
				while (oldIndex < oldSegments.Count && offset > oldSegments [oldIndex].Offset) {
					RemoveFolding (oldSegments [oldIndex]);
					oldIndex++;
				}
				
				if (oldIndex < oldSegments.Count && offset == oldSegments [oldIndex].Offset) {
					FoldSegment curSegment = oldSegments [oldIndex];
					curSegment.Length = newFoldSegment.Length;
					curSegment.Description = newFoldSegment.Description;
					if (newFoldSegment.IsFolded)
						curSegment.isFolded = true;
					if (curSegment.isFolded)
						newFoldedSegments.Add (curSegment);
				} else {
					LineSegment startLine = splitter.GetLineByOffset (offset);
					LineSegment endLine = splitter.GetLineByOffset (newFoldSegment.EndOffset);
					newFoldSegment.EndColumn = newFoldSegment.EndOffset - endLine.Offset + 1;
					newFoldSegment.Column = offset - startLine.Offset + 1;
					newFoldSegment.isAttached = true;
					if (newFoldSegment.IsFolded)
						newFoldedSegments.Add (newFoldSegment);
					foldSegmentTree.Add (newFoldSegment);
				}
				oldIndex++;
			}
			
			while (oldIndex < oldSegments.Count) {
				RemoveFolding (oldSegments [oldIndex]);
				oldIndex++;
			}
			if (worker != null) {
				Gtk.Application.Invoke (delegate {
					foldedSegments = newFoldedSegments;
					InformFoldTreeUpdated ();
				});
			} else {
				foldedSegments = newFoldedSegments;
				InformFoldTreeUpdated ();
			}
		}
		
		public void WaitForFoldUpdateFinished ()
		{
			while (FoldSegmentWorker.IsBusy)
				System.Threading.Thread.Sleep (10);
		}
		
		void InterruptFoldWorker ()
		{
			if (!FoldSegmentWorker.IsBusy)
				return;
			FoldSegmentWorker.CancelAsync ();
			WaitForFoldUpdateFinished ();
		}
		
		public void ClearFoldSegments ()
		{
			InterruptFoldWorker ();
			foldSegmentTree.RemoveListener (this);
			foldSegmentTree = new SegmentTree<FoldSegment> ();
			foldSegmentTree.InstallListener (this);
							
			InformFoldTreeUpdated ();
		}
		
		public IEnumerable<FoldSegment> GetFoldingsFromOffset (int offset)
		{
			if (offset < 0 || offset >= Length)
				return new FoldSegment[0];
			return foldSegmentTree.GetSegmentsAt (offset).Cast<FoldSegment> ();
		}
		
		public IEnumerable<FoldSegment> GetFoldingContaining (int lineNumber)
		{
			return GetFoldingContaining(this.GetLine (lineNumber));
		}
				
		public IEnumerable<FoldSegment> GetFoldingContaining (LineSegment line)
		{
			if (line == null)
				return new FoldSegment[0];
			return foldSegmentTree.GetSegmentsOverlapping (line.Offset, line.EditableLength).Cast<FoldSegment> ();
		}

		public IEnumerable<FoldSegment> GetStartFoldings (int lineNumber)
		{
			return GetStartFoldings (this.GetLine (lineNumber));
		}
		
		public IEnumerable<FoldSegment> GetStartFoldings (LineSegment line)
		{
			if (line == null)
				return new FoldSegment[0];
			return GetFoldingContaining (line).Where (fold => fold.StartLine == line);
		}

		public IEnumerable<FoldSegment> GetEndFoldings (int lineNumber)
		{
			return GetStartFoldings (this.GetLine (lineNumber));
		}
		
		public IEnumerable<FoldSegment> GetEndFoldings (LineSegment line)
		{
			foreach (FoldSegment segment in GetFoldingContaining (line)) {
				if (segment.EndLine.Offset == line.Offset)
					yield return segment;
			}
		}
		
		public int GetLineCount (FoldSegment segment)
		{
			return OffsetToLineNumber(segment.EndLine.Offset) - OffsetToLineNumber(segment.StartLine.Offset);
		}
		
		public void EnsureOffsetIsUnfolded (int offset)
		{
			bool needUpdate = false;
			foreach (FoldSegment fold in GetFoldingsFromOffset (offset).Where (f => f.Offset < offset && offset <= f.EndOffset)) {
				needUpdate |= fold.IsFolded;
				fold.IsFolded = false;
			}
			if (needUpdate) {
				RequestUpdate (new UpdateAll ());
				CommitDocumentUpdate ();
			}
		}
		
		internal void InformFoldTreeUpdated ()
		{
			var handler = FoldTreeUpdated;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}
		public event EventHandler FoldTreeUpdated;
		
		HashSet<FoldSegment> foldedSegments = new HashSet<FoldSegment> ();
		internal void InformFoldChanged (FoldSegmentEventArgs args)
		{
			if (args.FoldSegment.IsFolded) {
				foldedSegments.Add (args.FoldSegment);
			} else {
				foldedSegments.Remove (args.FoldSegment);
			}
			var handler = Folded;
			if (handler != null)
				handler (this, args);
		}
		public event EventHandler<FoldSegmentEventArgs> Folded;
		#endregion
		
		public event EventHandler<TextMarkerEvent> MarkerAdded;
		protected virtual void OnMarkerAdded (TextMarkerEvent e)
		{
			EventHandler<TextMarkerEvent> handler = this.MarkerAdded;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<TextMarkerEvent> MarkerRemoved;
		protected virtual void OnMarkerRemoved (TextMarkerEvent e)
		{
			EventHandler<TextMarkerEvent> handler = this.MarkerRemoved;
			if (handler != null)
				handler (this, e);
		}

		
		List<TextMarker> extendingTextMarkers = new List<TextMarker> ();
		public IEnumerable<LineSegment> LinesWithExtendingTextMarkers {
			get {
				return from marker in extendingTextMarkers where marker.LineSegment != null select marker.LineSegment;
			}
		}
		
		public void AddMarker (int lineNumber, TextMarker marker)
		{
			AddMarker (this.GetLine (lineNumber), marker);
		}
		
		public void AddMarker (LineSegment line, TextMarker marker)
		{
			AddMarker (line, marker, true);
		}
		
		public void AddMarker (LineSegment line, TextMarker marker, bool commitUpdate)
		{
			if (line == null || marker == null)
				return;
			if (marker is IExtendingTextMarker) {
				lock (extendingTextMarkers) {
					HeightChanged = true;
					extendingTextMarkers.Add (marker);
					extendingTextMarkers.Sort (CompareMarkers);
				}
			}
			line.AddMarker (marker);
			OnMarkerAdded (new TextMarkerEvent (line, marker));
			if (commitUpdate)
				this.CommitLineUpdate (line);
		}
		
		static int CompareMarkers (TextMarker left, TextMarker right)
		{
			if (left.LineSegment == null || right.LineSegment == null)
				return 0;
			return left.LineSegment.Offset.CompareTo (right.LineSegment.Offset);
		}
		
		public void RemoveMarker (TextMarker marker)
		{
			RemoveMarker (marker, true);
		}
		
		public void RemoveMarker (TextMarker marker, bool updateLine)
		{
			if (marker == null)
				return;
			var line = marker.LineSegment;
			if (line == null)
				return;
			if (marker is IExtendingTextMarker) {
				lock (extendingTextMarkers) {
					HeightChanged = true;
					extendingTextMarkers.Remove (marker);
				}
			}
			
			line.RemoveMarker (marker);
			OnMarkerRemoved (new TextMarkerEvent (line, marker));
			if (updateLine)
				this.CommitLineUpdate (line);
		}
		
		public void RemoveMarker (int lineNumber, Type type)
		{
			RemoveMarker (this.GetLine (lineNumber), type);
		}
		
		public void RemoveMarker (LineSegment line, Type type)
		{
			RemoveMarker (line, type, true);
		}
		
		public void RemoveMarker (LineSegment line, Type type, bool updateLine)
		{
			if (line == null || type == null)
				return;
			if (typeof(IExtendingTextMarker).IsAssignableFrom (type)) {
				lock (extendingTextMarkers) {
					HeightChanged = true;
					foreach (TextMarker marker in line.Markers.Where (marker => marker is IExtendingTextMarker)) {
						extendingTextMarkers.Remove (marker);
					}
				}
			}
			line.RemoveMarker (type);
			if (updateLine)
				this.CommitLineUpdate (line);
		}
		
		void HandleSplitterLineSegmentTreeLineRemoved (object sender, LineEventArgs e)
		{
			foreach (TextMarker marker in e.Line.Markers) {
				if (marker is IExtendingTextMarker) {
					lock (extendingTextMarkers) {
						HeightChanged = true;
						extendingTextMarkers.Remove (marker);
					}
					UnRegisterVirtualTextMarker ((IExtendingTextMarker)marker);
				}
			}
		}
		
		public bool Contains (int offset)
		{
			return new Segment (0, Length).Contains (offset);
		}
		
		public bool Contains (ISegment segment)
		{
			return new Segment (0, Length).Contains (segment);
		}
		
		
		public DocumentLocation LogicalToVisualLocation (TextEditorData editor, DocumentLocation location)
		{
			int line = LogicalToVisualLine (location.Line);
			LineSegment lineSegment = this.GetLine (location.Line);
			int column = lineSegment != null ? lineSegment.GetVisualColumn (editor, location.Column) : location.Column;
			return new DocumentLocation (line, column);
		}
		
		public int LogicalToVisualLine (int logicalLine)
		{
			int result = logicalLine;
			LineSegment line = GetLine (result) ?? GetLine (LineCount);
			int maxOffset = 0;
			int lineOffset = line.Offset;
			foreach (FoldSegment segment in foldedSegments) {
				int startLineOffset = segment.StartLine.Offset;
				if (startLineOffset < lineOffset && segment.Offset >= maxOffset) {
					result -= GetLineCount (segment);
					maxOffset = segment.EndOffset;
				}
				if (startLineOffset > lineOffset)
					break;
			}
			return result;
		}

		public int VisualToLogicalLine (int visualLineNumber)
		{
			if (visualLineNumber < DocumentLocation.MinLine)
				return DocumentLocation.MinLine;
			
			int result = visualLineNumber;
			int maxOffset = 0;
			LineSegment line = GetLine (result);
			foreach (FoldSegment segment in foldedSegments) {
				if (segment.Offset < maxOffset)
					continue;
				if ((line == null || segment.StartLine.Offset < line.Offset)) {
					result += GetLineCount (segment);
					if (line != null)
						line = GetLine (result);
					maxOffset = segment.EndOffset;
				}
				if (line != null && segment.Offset > line.Offset)
					break;
			}
			return result;
		}
		#region Update logic
		List<DocumentUpdateRequest> updateRequests = new List<DocumentUpdateRequest> ();
		
		public IEnumerable<DocumentUpdateRequest> UpdateRequests {
			get {
				return updateRequests;
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
		
		public bool IsEmptyLine (LineSegment line)
		{
			for (int i = 0; i < line.EditableLength; i++) {
				char ch = GetCharAt (line.Offset + i);
				if (!Char.IsWhiteSpace (ch)) 
					return false;
			}
			return true;
		}

		
		public int GetMatchingBracketOffset (int offset)
		{
			return GetMatchingBracketOffset (null, offset);
		}
		
		public int GetMatchingBracketOffset (System.ComponentModel.BackgroundWorker worker, int offset)
		{
			if (offset < 0 || offset >= Length)
				return -1;
			char ch = GetCharAt (offset);
			int bracket = openBrackets.IndexOf (ch);
			int result;
			if (bracket >= 0) {
				result = SearchMatchingBracketForward (worker, offset + 1, bracket);
			} else {
				bracket = closingBrackets.IndexOf (ch);
				if (bracket >= 0) {
					result = SearchMatchingBracketBackward (worker, offset - 1, bracket);
				} else {
					result = -1;
				}
			}
			return result;
		}
		IBracketMatcher bracketMatcher = new DefaultBracketMatcher ();
		public IBracketMatcher BracketMatcher {
			get {
				return bracketMatcher;
			}
			set {
				Debug.Assert (value != null);
				bracketMatcher = value;
			}
		}

		
		int SearchMatchingBracketForward (System.ComponentModel.BackgroundWorker worker, int offset, int bracket)
		{
			return bracketMatcher.SearchMatchingBracketForward (worker, this, offset, closingBrackets[bracket], openBrackets[bracket]);
		}
		
		int SearchMatchingBracketBackward (System.ComponentModel.BackgroundWorker worker, int offset, int bracket)
		{
			return bracketMatcher.SearchMatchingBracketBackward (worker, this, offset, openBrackets[bracket], closingBrackets[bracket]);
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
		
		public static void UpdateSegments (IEnumerable<ISegment> segments, ReplaceEventArgs args)
		{
			int delta = -args.Count + (!string.IsNullOrEmpty (args.Value) ? args.Value.Length : 0);
			foreach (ISegment segment in segments) {
				if (args.Offset < segment.Offset) {
					segment.Offset += delta;
				} else if (args.Offset <= segment.EndOffset) {
					segment.Length += delta;
				}
			}
		}
		
		public static void RemoveTrailingWhitespaces (TextEditorData data, LineSegment line)
		{
			if (line == null)
				return;
			int whitespaces = 0;
			for (int i = line.EditableLength - 1; i >= 0; i--) {
				if (Char.IsWhiteSpace (data.Document.GetCharAt (line.Offset + i))) {
					whitespaces++;
				} else {
					break;
				}
			}
			
			if (whitespaces > 0) {
				data.Remove (line.Offset + line.EditableLength - whitespaces, whitespaces);
				data.Caret.CheckCaretPosition ();
			}
		}
		#endregion

		public bool IsInUndo {
			get {
				return isInUndo;
			}
		}
		
		Dictionary<int, IExtendingTextMarker> virtualTextMarkers = new Dictionary<int, IExtendingTextMarker> ();
		public void RegisterVirtualTextMarker (int lineNumber, IExtendingTextMarker marker)
		{
			virtualTextMarkers[lineNumber] = marker;
		}
		
		public IExtendingTextMarker GetExtendingTextMarker (int lineNumber)
		{
			IExtendingTextMarker result;
			if (virtualTextMarkers.TryGetValue (lineNumber, out result))
				return result;
			return null;
		}
		
		/// <summary>
		/// un register virtual text marker.
		/// </summary>
		/// <param name='marker'>
		/// marker.
		/// </param>
		public void UnRegisterVirtualTextMarker (IExtendingTextMarker marker)
		{
			List<int> keys = new List<int> (from pair in virtualTextMarkers where pair.Value == marker select pair.Key);
			keys.ForEach (key => { virtualTextMarkers.Remove (key); CommitLineUpdate (key); });
		}
		
		
		#region Diff
		int[] GetDiffCodes (ref int codeCounter, Dictionary<string, int> codeDictionary)
		{
			int i = 0;
			int[] result = new int[LineCount];
			foreach (LineSegment line in Lines) {
				string lineText = buffer.GetTextAt (line.Offset, line.EditableLength);
				int curCode;
				if (!codeDictionary.TryGetValue (lineText, out curCode)) {
					codeDictionary[lineText] = curCode = ++codeCounter;
				}
				result[i] = curCode;
				i++;
			}
			return result;
		}
		
		public IEnumerable<Hunk> Diff (Document changedDocument)
		{
			Dictionary<string, int> codeDictionary = new Dictionary<string, int> ();
			int codeCounter = 0;
			return Mono.TextEditor.Utils.Diff.GetDiff<int> (this.GetDiffCodes (ref codeCounter, codeDictionary),
				changedDocument.GetDiffCodes (ref codeCounter, codeDictionary));
		}
		#endregion
		
		#region Partitioner
		IDocumentPartitioner partitioner;
		public IDocumentPartitioner Partitioner {
			get { 
				return partitioner; 
			}
			set {
				partitioner = value;
				partitioner.Document = this; 
			}
		}
		
		public IEnumerable<TypedSegment> GetPartitions (int offset, int length)
		{
			if (Partitioner == null)
				return new TypedSegment[0];
			return Partitioner.GetPartitions (offset, length);
		}
		
		public IEnumerable<TypedSegment> GetPartitions (ISegment segment)
		{
			if (Partitioner == null)
				return new TypedSegment[0];
			return Partitioner.GetPartitions (segment);
		}
		
		public TypedSegment GetPartition (int offset)
		{
			if (Partitioner == null)
				return null;
			return Partitioner.GetPartition (offset);
		}
		
		public TypedSegment GetPartition (DocumentLocation location)
		{
			if (Partitioner == null)
				return null;
			return Partitioner.GetPartition (location);
		}
		
		public TypedSegment GetPartition (int line, int column)
		{
			if (Partitioner == null)
				return null;
			return Partitioner.GetPartition (line, column);
		}
		#endregion
		
		#region ContentLoaded 
		// The problem: Action to perform on a newly opened text editor, but content didn't get loaded because autosave file exist.
		//              At this point the document is open, but the content didn't yet have loaded - therefore the action on the conent can't be perfomed.
		// Solution: Perform the action after the user did choose load autosave or not. 
		//           This is done by the RunWhenLoaded method. Text editors should call the InformLoadComplete () when the content has successfully been loaded
		//           at that point the outstanding actions are run.
		bool isLoaded;
		List<Action> loadedActions = new List<Action> ();
		
		/// <summary>
		/// Gets a value indicating whether this instance is loaded.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is loaded; otherwise, <c>false</c>.
		/// </value>
		public bool IsLoaded {
			get { return isLoaded; }
		}
		
		/// <summary>
		/// Informs the document when the content is loaded. All outstanding actions are executed.
		/// </summary>
		public void InformLoadComplete ()
		{
			if (isLoaded)
				return;
			isLoaded = true;
			loadedActions.ForEach (act => act ());
			loadedActions = null;
		}
		
		/// <summary>
		/// Performs an action when the content is loaded.
		/// </summary>
		/// <param name='action'>
		/// The action to run.
		/// </param>
		public void RunWhenLoaded (Action action)
		{
			if (IsLoaded) {
				action ();
				return;
			}
			loadedActions.Add (action);
		}
		#endregion
	}
	
	public delegate bool ReadOnlyCheckDelegate (int line);
}
