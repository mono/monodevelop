//
// ByteBuffer.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;

namespace Mono.MHex.Data
{
	class ByteBuffer
	{
		PieceTable pieceTable = new PieceTable ();
		ByteAddBuffer addBuffer = new ByteAddBuffer ();
		IBuffer buffer = new ArrayBuffer (new byte [0]);
		long lastChangeOffset;

		class ByteAddBuffer : IBuffer
		{
			public List<byte> Bytes { get; } = new List<byte> ();

			public long Length => Bytes.Count;

			public byte [] GetBytes (long offset, int count)
			{
				byte [] result = new byte [count];
				Bytes.CopyTo ((int)offset, result, 0, count);
				return result;
			}
		}

		public IBuffer Buffer {
			get {
				return this.buffer;
			}
			set {
				var oldCount = Length;
				this.buffer = value;
				pieceTable.SetBuffer (buffer);
				OnReplaced (new ReplaceEventArgs (0, oldCount, Bytes));
			}
		}

		public IBuffer AddBuffer {
			get {
				return this.addBuffer;
			}
		}

		public long Length {
			get {
				return pieceTable.Length;
			}
		}

		public byte [] Bytes {
			get {
				return GetBytes (0, (int)Length);
			}
		}

		public byte [] GetBytes (long offset, int count)
		{
			if (count == 0)
				return new byte [0];
			var node = pieceTable.GetTreeNodeAtOffset (offset);
			if (node == null)
				return new byte [0];
			long nodeOffset = node.value.CalcOffset (node);
			long nodeEndOffset = nodeOffset + node.value.Length;
			if (offset + count < nodeEndOffset)
				return node.value.GetBytes (this, nodeOffset, offset, count);
			byte [] nodeBytes = node.value.GetBytes (this, nodeOffset, offset, (int)(nodeEndOffset - offset));

			byte [] result = new byte [count];
			if (nodeBytes.Length > 0) {
				nodeBytes.CopyTo (result, 0);
				GetBytes (offset + nodeBytes.Length, count - nodeBytes.Length).CopyTo (result, nodeBytes.Length);
			}
			return result;
		}

		public byte GetByte (long offset)
		{
			return GetBytes (offset, 1) [0];
		}

		public void Replace (long offset, long count, params byte [] data)
		{
			if (!isInUndo)
				BeginAtomicUndo ();
			if (count > 0)
				pieceTable.Remove (offset, count);
			if (data != null && data.Length > 0) {
				int addBufferOffset = addBuffer.Bytes.Count;
				long length = data.Length;
				addBuffer.Bytes.AddRange (data);
				pieceTable.Insert (offset, addBufferOffset, length);
			}
			lastChangeOffset = offset + data.Length;
			OnReplaced (new ReplaceEventArgs (offset, count, data));
			if (!isInUndo)
				EndAtomicUndo ();
		}

		public void Insert (long offset, params byte [] data)
		{
			if (!isInUndo)
				BeginAtomicUndo ();
			int addBufferOffset = addBuffer.Bytes.Count;
			long length = data.Length;
			addBuffer.Bytes.AddRange (data);
			pieceTable.Insert (offset, addBufferOffset, length);
			lastChangeOffset = offset + data.Length;
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
			lastChangeOffset = offset;
			OnReplaced (new ReplaceEventArgs (offset, count, null));
			if (!isInUndo)
				EndAtomicUndo ();
			if (Length == 0)
				Insert (0, new byte [] { 0 });
		}

		protected virtual void OnReplaced (ReplaceEventArgs args)
		{
			if (Replaced != null)
				Replaced (this, args);
		}

		Stack<UndoOperation> undoStack = new Stack<UndoOperation> ();
		Stack<UndoOperation> redoStack = new Stack<UndoOperation> ();
		UndoOperation currentAtomicOperation;

		bool isInUndo = false;
		int atomicUndoLevel;

		public bool CanUndo {
			get {
				return undoStack.Count > 0;
			}
		}

		public bool CanRedo {
			get {
				return redoStack.Count > 0;
			}
		}

		public void BeginAtomicUndo ()
		{
			if (currentAtomicOperation == null) {
				currentAtomicOperation = new UndoOperation ();
				currentAtomicOperation.undoNode = pieceTable.tree.Root.Clone ();
				currentAtomicOperation.undoCaret = lastChangeOffset;
			}
			atomicUndoLevel++;
		}

		public void EndAtomicUndo ()
		{
			atomicUndoLevel--;

			if (atomicUndoLevel == 0 && currentAtomicOperation != null) {
				currentAtomicOperation.redoNode = pieceTable.tree.Root.Clone ();
				currentAtomicOperation.redoCaret = lastChangeOffset;
				undoStack.Push (currentAtomicOperation);
				redoStack.Clear ();
				currentAtomicOperation = null;
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
			OnUndone (new UndoOperationEventArgs (operation.undoCaret));
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
			OnRedone (new UndoOperationEventArgs (operation.redoCaret));
		}

		internal protected virtual void OnRedone (UndoOperationEventArgs e)
		{
			EventHandler<UndoOperationEventArgs> handler = this.Redone;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<UndoOperationEventArgs> Redone;

		public event EventHandler<ReplaceEventArgs> Replaced;

		public class UndoOperation
		{
			internal RedBlackTree<PieceTable.TreeNode>.RedBlackTreeNode undoNode;
			internal long undoCaret;

			internal RedBlackTree<PieceTable.TreeNode>.RedBlackTreeNode redoNode;
			internal long redoCaret;

			public UndoOperation ()
			{
			}

			public virtual void Undo (ByteBuffer data)
			{
				if (undoNode == null)
					throw new NullReferenceException ("undoNode == null");
				data.pieceTable.tree.Root = undoNode;
			}

			public virtual void Redo (ByteBuffer data)
			{
				if (redoNode == null)
					throw new NullReferenceException ("redoNode == null");
				data.pieceTable.tree.Root = redoNode;
			}
		}
	}
}