// 
// Selection.cs
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
	enum SelectionMode {
		Normal,
		Block
	}
	
	class Selection : IEquatable <Selection>
	{
		long anchor;
		public long Anchor {
			get {
				return anchor;
			}
			set {
				if (anchor != value) {
					anchor = value;
					OnChanged ();
				}
			}
		}
		
		long lead;
		public long Lead {
			get {
				return lead;
			}
			set {
				if (lead != value) {
					lead = value;
					OnChanged ();
				}
			}
		}
		
		public SelectionMode SelectionMode {
			get;
			set;
		}
		
		public ISegment Segment {
			get {
				return Anchor <= Lead ? new Segment (Anchor, Lead - Anchor + 1) : new Segment (Lead, Anchor - Lead + 1);
			}
		}
		
		public bool Contains (long offset)
		{
			return anchor <= offset && offset <= lead || lead <= offset && offset <= anchor;
		}
		
		public Selection (long anchor, long lead)
		{
			this.anchor = anchor;
			this.lead = lead;
		}
		
		#region IEquatable<Selection> implementation
		public bool Equals (Selection other)
		{
			if (other == null)
				return false;
			if (ReferenceEquals (this, other))
				return true;
			return Anchor == other.Anchor && Lead == other.Lead;
		}
		#endregion
		
		public override string ToString ()
		{
			return string.Format ("[Selection: Anchor={0}, Lead={1}, SelectionMode={2}]", Anchor, Lead, SelectionMode);
		}
		
		protected virtual void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public event EventHandler Changed;
	}
}
