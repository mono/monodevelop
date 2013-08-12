// 
// Caret.cs
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

namespace Mono.MHex.Data
{
	class Caret
	{
		HexEditorData data;
		
		long offset;
		public long Offset {
			get { 
				return offset; 
			}
			set { 
				value = System.Math.Min (data.Length, System.Math.Max (0, value));
				if (offset != value) {
					long old = offset;
					offset = value; 
					subPosition = 0;
					OnOffsetChanged (new CaretLocationEventArgs (old)); 
				}
			}
		}
		
		int subPosition;
		public int SubPosition {
			get { 
				return subPosition; 
			}
			set { 
				if (subPosition != value) {
					subPosition = value; 
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public int MaxSubPosition {
			get {
				return 1;
			}
		}
		
		bool inTextEditor;
		public bool InTextEditor {
			get {
				return inTextEditor;
			}
			set {
				inTextEditor = value;
				SubPosition = 0;
			}
		}
		
		public long Line {
			get {
				return Offset / data.BytesInRow;
			}
		}
		
		bool isInsertMode;
		public bool IsInsertMode {
			get { 
				return isInsertMode; 
			}
			set {
				if (isInsertMode != value) {
					isInsertMode = value; 
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public bool PreserveSelection {
			get;
			set;
		}
		
		public bool AutoScrollToCaret {
			get;
			set;
		}
		
		public Caret (HexEditorData data)
		{
			this.data = data;
			PreserveSelection = false;
			AutoScrollToCaret = true;
		}
		
		protected virtual void OnOffsetChanged (CaretLocationEventArgs e)
		{
			EventHandler<CaretLocationEventArgs> handler = this.OffsetChanged;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler<CaretLocationEventArgs> OffsetChanged;
		
		protected virtual void OnChanged (EventArgs e)
		{
			EventHandler handler = this.Changed;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler Changed;
	}
	
	[Serializable]
	sealed class CaretLocationEventArgs : EventArgs
	{
		public long OldOffset {
			get;
			set;
		}
		public CaretLocationEventArgs (long oldOffset)
		{
			this.OldOffset = oldOffset;
		}
		
	}
}
