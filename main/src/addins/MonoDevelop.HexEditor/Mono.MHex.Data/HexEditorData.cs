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
		ByteBuffer byteBuffer;
		PieceTable pieceTable = new PieceTable ();

		public event EventHandler<UndoOperationEventArgs> Undone;
		public event EventHandler<UndoOperationEventArgs> Redone;
		public event EventHandler Changed;

		public ByteBuffer ByteBuffer {
			get => byteBuffer;
			set {
				if (byteBuffer != value) {
					if (byteBuffer != null)
						UnsubscribeBufferEvents ();
					byteBuffer = value;
					if (byteBuffer != null)
						SubscribeBufferEvents ();
					OnChanged ();
				}
			}
		}

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
				return ByteBuffer.Length;
			}
		}

		public byte [] Bytes {
			get {
				return ByteBuffer.GetBytes (0, (int)Length);
			}
		}

		public byte [] GetBytes (long offset, int count)
		{
			return ByteBuffer.GetBytes (offset, count);
		}

		public byte GetByte (long offset)
		{
			return ByteBuffer.GetBytes (offset, 1) [0];
		}

		public HexEditorData ()
		{
			ByteBuffer = new ByteBuffer ();
			Caret = new Caret (this);
			VAdjustment = new ScrollAdjustment ();
			HAdjustment = new ScrollAdjustment ();
		}

		void SubscribeBufferEvents ()
		{
			byteBuffer.Undone += ByteBuffer_Undone;
			byteBuffer.Redone += ByteBuffer_Redone;
			byteBuffer.Replaced += ByteBuffer_Replaced;
		}

		void UnsubscribeBufferEvents ()
		{
			byteBuffer.Undone -= ByteBuffer_Undone;
			byteBuffer.Redone -= ByteBuffer_Redone;
			byteBuffer.Replaced -= ByteBuffer_Replaced;
		}

		void ByteBuffer_Undone (object sender, UndoOperationEventArgs e)
		{
			Caret.Offset = e.BufferOffset;
			Undone?.Invoke (this, e);
		}

		void ByteBuffer_Redone (object sender, UndoOperationEventArgs e)
		{
			Caret.Offset = e.BufferOffset;
			Redone?.Invoke (this, e);
		}

		void ByteBuffer_Replaced (object sender, ReplaceEventArgs e)
		{
			OnChanged ();
		}

		void OnChanged ()
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		public IBuffer Buffer {
			get {
				return ByteBuffer.Buffer;
			}
			set {
				ByteBuffer.Buffer = value;
				OnBufferChanged (EventArgs.Empty);
			}
		}

		protected virtual void OnBufferChanged (EventArgs e)
		{
			EventHandler handler = this.BufferChanged;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler BufferChanged;

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
				ByteBuffer.Remove (MainSelection.Segment);
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
	}
}
