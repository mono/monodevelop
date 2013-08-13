// 
// HexEditorData.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Xwt;
using System.Collections.Generic;

namespace Mono.MHex.Data
{
	class HexEditorData
	{
		public ScrollAdjustment HAdjustment {
			get;
			set;
		}
		
		public ScrollAdjustment VAdjustment {
			get;
			set;
		}
		
		public Caret Caret {
			get;
			private set;
		}
		
		public int BytesInRow {
			get;
			set;
		}
		
		public double LineHeight {
			get;
			set;
		}
		
		public EditMode EditMode {
			get;
			set;
		}
			
		List<long> bookmarks = new List<long> ();
		public List<long> Bookmarks {
			get {
				return bookmarks;
			}
		}
		
		public long Length {
			get {
				return pieceTable.Length;
			}
		}
		
		public byte[] Bytes {
			get {
				return GetBytes (0, (int)Length);
			}
		}
		
		public byte[] GetBytes (long offset, int count)
		{
			if (count == 0)
				return new byte[0];
			var node = pieceTable.GetTreeNodeAtOffset (offset);
			if (node == null)
				return new byte[0];
			long nodeOffset = node.value.CalcOffset (node);
			long nodeEndOffset = nodeOffset + node.value.Length;
			if (offset + count < nodeEndOffset)
				return node.value.GetBytes (this, nodeOffset, offset, count);
			byte[] nodeBytes = node.value.GetBytes (this, nodeOffset, offset, (int)(nodeEndOffset - offset));
			
			byte[] result = new byte[count];
			if (nodeBytes.Length > 0) {
				nodeBytes.CopyTo (result, 0);
				GetBytes (offset + nodeBytes.Length, count - nodeBytes.Length).CopyTo (result, nodeBytes.Length);
			}
			return result;
		}
		
		public byte GetByte (long offset)
		{
			return GetBytes (offset, 1)[0];
		}
		
		public HexEditorData ()
		{
			Caret = new Caret (this);
			VAdjustment = new ScrollAdjustment ();
			HAdjustment = new ScrollAdjustment ();
		}
		
		PieceTable pieceTable = new PieceTable ();
		internal IBuffer buffer;
		
		public IBuffer Buffer {
			get { 
				return this.buffer; 
			}
			set { 
				this.buffer = value; 
				pieceTable.SetBuffer (buffer);
				OnBufferChanged (EventArgs.Empty);
			}
		}
		
		internal List<byte> addBuffer = new List<byte> ();
		
		protected virtual void OnBufferChanged (EventArgs e)
		{
			EventHandler handler = this.BufferChanged;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler BufferChanged;
		
		public void Replace (long offset, long count, params byte[] data)
		{
			if (!isInUndo) 
				BeginAtomicUndo ();
			if (count > 0)
				pieceTable.Remove (offset, count);
			if (data != null && data.Length > 0) {
				int addBufferOffset = addBuffer.Count;
				long length = data.Length;
				addBuffer.AddRange (data);
				pieceTable.Insert (offset, addBufferOffset, length);
			}
			OnReplaced (new ReplaceEventArgs (offset, count, data));
			if (!isInUndo) 
				EndAtomicUndo ();
		}
		
		public void Insert (long offset, params byte[] data)
		{
			if (!isInUndo) 
				BeginAtomicUndo ();
			int addBufferOffset = addBuffer.Count;
			long length = data.Length;
			addBuffer.AddRange (data);
			pieceTable.Insert (offset, addBufferOffset, length);
			OnReplaced (new ReplaceEventArgs (offset, 0, data));
			if (!isInUndo) 
				EndAtomicUndo ();
		}
		
		public void Remove (ISegment segment)
		{
			Remove (segment.Offset, segment.Length);
		}
		
		public void Remove (long offset, long count)
		{
			if (!isInUndo) 
				BeginAtomicUndo ();
			pieceTable.Remove (offset, count);
			OnReplaced (new ReplaceEventArgs (offset, count, null));
			if (!isInUndo) 
				EndAtomicUndo ();
			if (Length == 0)
				Insert (0, new byte[] {0});
		}
		
		public void UpdateLine (long line)
		{
			Updates.Add (new LineUpdateRequest (line));
			OnUpdateRequested (EventArgs.Empty);
		}
		
		public void UpdateMargin (Type marginType, long line)
		{
			Updates.Add (new MarginLineUpdateRequest (marginType, line));
			OnUpdateRequested (EventArgs.Empty);
		}
		
		internal List<UpdateRequest> Updates = new List<UpdateRequest> ();
		
		protected virtual void OnUpdateRequested (EventArgs e)
		{
			EventHandler handler = this.UpdateRequested;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler UpdateRequested;
		
		protected virtual void OnReplaced (ReplaceEventArgs args)
		{
			if (Replaced != null)
				Replaced (this, args);
		}
		
		public event EventHandler<ReplaceEventArgs> Replaced;
		
		#region Selection
		public bool IsSomethingSelected {
			get {
				return MainSelection != null && MainSelection.Anchor != MainSelection.Lead; 
			}
		}
		
		public SelectionMode SelectionMode {
			get {
				return MainSelection != null ? MainSelection.SelectionMode : SelectionMode.Normal;
			}
			set {
				MainSelection.SelectionMode = value;
			}
		}
		
		Selection mainSelection = null;
		public Selection MainSelection {
			get {
				return mainSelection;
			}
			set {
				if (mainSelection == null && value == null)
					return;
				if (mainSelection == null && value != null || mainSelection != null && value == null || !mainSelection.Equals (value)) {
					mainSelection = value;
					if (mainSelection != null) {
						mainSelection.Changed += delegate {
							OnSelectionChanged (EventArgs.Empty);
						};
					}
					OnSelectionChanged (EventArgs.Empty);
				}
			}
		}
		
		public void ClearSelection ()
		{
			if (!this.IsSomethingSelected)
				return;
			MainSelection = null;
			OnSelectionChanged (EventArgs.Empty);
		}
		
		public void SetSelection (long anchor, long lead)
		{
			anchor = System.Math.Min (Length, System.Math.Max (0, anchor));
			lead = System.Math.Min (Length, System.Math.Max (0, lead));
			MainSelection = new Selection (anchor, lead);
		}
		
		public void DeleteSelection ()
		{
			if (!this.IsSomethingSelected)
				return;
			long start = MainSelection.Segment.Offset;
			switch (MainSelection.SelectionMode) {
			case SelectionMode.Normal:
				Remove (MainSelection.Segment);
				break;
			case SelectionMode.Block:
				throw new NotImplementedException ();
			}
			
			MainSelection = null;
			Caret.Offset = start;
			OnSelectionChanged (EventArgs.Empty);
			
		}
		
		public void ExtendSelectionTo (long offset)
		{
			offset = System.Math.Min (Length, System.Math.Max (0, offset));
			if (MainSelection == null) 
				MainSelection = new Selection (offset, offset);
			MainSelection.Lead = offset;
		}
		
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
			if (SelectionChanged != null) 
				SelectionChanged (this, args);
		}
		#endregion
		
		#region Undo/Redo
		public class CaretPosition 
		{
			long Offset {
				get;
				set;
			}
			int SubPosition {
				get;
				set;
			}
			
			public CaretPosition (Caret caret)
			{
				Offset = caret.Offset;
				SubPosition = caret.SubPosition;
			}
			
			public void Set (Caret caret)
			{
				caret.Offset = Offset;
				caret.SubPosition = SubPosition;
			}
		}
		
		public class UndoOperation
		{
			internal RedBlackTree<PieceTable.TreeNode>.RedBlackTreeNode undoNode;
			internal CaretPosition undoCaret;
			
			internal RedBlackTree<PieceTable.TreeNode>.RedBlackTreeNode redoNode;
			internal CaretPosition redoCaret;
			
			public UndoOperation()
			{
			}
			
			public virtual void Undo (HexEditorData data)
			{
				if (undoNode == null)
					throw new NullReferenceException ("undoNode == null");
				data.pieceTable.tree.Root = undoNode;
				undoCaret.Set (data.Caret);
			}
			
			public virtual void Redo (HexEditorData data)
			{
				if (redoNode == null)
					throw new NullReferenceException ("redoNode == null");
				data.pieceTable.tree.Root = redoNode;
				redoCaret.Set (data.Caret);
			}
		}
		
		Stack<UndoOperation> undoStack = new Stack<UndoOperation> ();
		Stack<UndoOperation> redoStack = new Stack<UndoOperation> ();
		UndoOperation currentAtomicOperation;
		
		public bool EnableUndo {
			get {
				return undoStack.Count > 0;
			}
		}
		
		public bool EnableRedo {
			get {
				return redoStack.Count > 0;
			}
		}
		
		bool isInUndo = false;
		int atomicUndoLevel;
		public void BeginAtomicUndo ()
		{
			if (currentAtomicOperation == null) {
				currentAtomicOperation = new UndoOperation ();
				currentAtomicOperation.undoNode = pieceTable.tree.Root.Clone ();
				currentAtomicOperation.undoCaret = new CaretPosition (Caret);
			}
			atomicUndoLevel++;
		}
		
		public void EndAtomicUndo ()
		{
			atomicUndoLevel--;
			
			if (atomicUndoLevel == 0 && currentAtomicOperation != null) {
				currentAtomicOperation.redoNode = pieceTable.tree.Root.Clone ();
				currentAtomicOperation.redoCaret = new CaretPosition (Caret);
				undoStack.Push (currentAtomicOperation);
				redoStack.Clear ();
				currentAtomicOperation = null;
			}
		}
		public class UndoOperationEventArgs : EventArgs
		{
			public UndoOperation Operation {
				get;
				private set;
			}
			
			public UndoOperationEventArgs (UndoOperation operation)
			{
				this.Operation = operation;
			}
		}
		public void Undo ()
		{
			if (undoStack.Count <= 0)
				return;
			isInUndo = true;
			UndoOperation operation = undoStack.Pop ();
			operation.Undo (this);
			redoStack.Push (operation);
			isInUndo = false;
			OnUndone (new UndoOperationEventArgs (operation));
		}
		
		internal protected virtual void OnUndone (UndoOperationEventArgs e)
		{
			EventHandler<UndoOperationEventArgs> handler = this.Undone;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler<UndoOperationEventArgs> Undone;
		
		public void Redo ()
		{
			if (redoStack.Count <= 0)
				return;
			isInUndo = true;
			UndoOperation operation = redoStack.Pop ();
			operation.Redo (this);
			undoStack.Push (operation);
			isInUndo = false;
			OnRedone (new UndoOperationEventArgs (operation));
		}
		
		internal protected virtual void OnRedone (UndoOperationEventArgs e)
		{
			EventHandler<UndoOperationEventArgs> handler = this.Redone;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler<UndoOperationEventArgs> Redone;
		
		#endregion
	}
}
