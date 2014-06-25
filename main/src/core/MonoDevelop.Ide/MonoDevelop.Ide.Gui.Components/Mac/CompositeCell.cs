// 
// CompositeCell.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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
using CoreGraphics;

#if MAC
using System;
using AppKit;
using Foundation;
using Xwt.Backends;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class CompositeCell: NSCell, ICopiableObject
	{
		TreeItem val;
		const int PaddingLeft = 3;

		ImageTableCell imageCell = new ImageTableCell ();
		NSTextFieldCell textCell = new NSTextFieldCell {
			TruncatesLastVisibleLine = true
		};

		static CompositeCell ()
		{
			Util.MakeCopiable<CompositeCell> ();
		}

		public CompositeCell ()
		{
		}
		
		public CompositeCell (IntPtr p): base (p)
		{
		}

		void ICopiableObject.CopyFrom (object other)
		{
			var ob = (CompositeCell)other;
			val = ob.val;
			Fill ();
		}
		
		public override NSObject ObjectValue {
			get {
				return val;
			}
			set {
				val = value as TreeItem;
				Fill ();
			}
		}

		public void Fill ()
		{
			if (val != null) {
				textCell.AttributedStringValue = val.Markup;
				imageCell.Fill (val);
			}
		}
		
		IEnumerable<NSCell> VisibleCells {
			get {
				yield return imageCell;
				yield return textCell;
			}
		}

		CoreGraphics.CGSize CalcSize ()
		{
			nfloat w = 0;
			nfloat h = 0;
			foreach (NSCell c in VisibleCells) {
				var s = c.CellSize;
				w += s.Width;
				if (s.Height > h)
					h = s.Height;
			}
			return new CGSize (w, h);
		}

		public override CGSize CellSizeForBounds (CGRect bounds)
		{
			return CalcSize ();
		}
		
		public override NSCellStateValue State {
			get {
				return base.State;
			}
			set {
				base.State = value;
				foreach (NSCell c in VisibleCells)
					c.State = value;
			}
		}
		
		public override bool Highlighted {
			get {
				return base.Highlighted;
			}
			set {
				base.Highlighted = value;
				foreach (NSCell c in VisibleCells)
					c.Highlighted = value;
			}
		}
		
		public override void DrawInteriorWithFrame (CGRect cellFrame, NSView inView)
		{
			foreach (CellPos cp in GetCells(cellFrame))
				cp.Cell.DrawInteriorWithFrame (cp.Frame, inView);
		}
		
		public override void Highlight (bool flag, CGRect withFrame, NSView inView)
		{
			foreach (CellPos cp in GetCells(withFrame)) {
				cp.Cell.Highlight (flag, cp.Frame, inView);
			}
		}
		
		public override NSCellHit HitTest (NSEvent forEvent, CGRect inRect, NSView ofView)
		{
			foreach (CellPos cp in GetCells(inRect)) {
				var h = cp.Cell.HitTest (forEvent, cp.Frame, ofView);
				if (h != NSCellHit.None)
					return h;
			}
			return NSCellHit.None;
		}

		public override void SetCellAttribute (NSCellAttribute aParameter, nint value)
		{
			base.SetCellAttribute (aParameter, value);
			foreach (NSCell c in VisibleCells)
				c.SetCellAttribute (aParameter, value);
		}

		public CGRect GetCellRect (CGRect cellFrame, NSCell cell)
		{
			foreach (var c in GetCells (cellFrame)) {
				if (c.Cell == cell)
					return c.Frame;
			}
			return CGRect.Empty;
		}

		IEnumerable<CellPos> GetCells (CGRect cellFrame)
		{
			cellFrame.X += PaddingLeft;
			cellFrame.Width -= PaddingLeft;

			foreach (NSCell c in VisibleCells) {
				var s = c.CellSize;
				var w = (nfloat) Math.Min (cellFrame.Width, s.Width);
				var y = cellFrame.Y + (int) ((cellFrame.Height - s.Height) / 2);
				CGRect f = new CGRect (cellFrame.X, y, w, s.Height);
				cellFrame.X += w;
				cellFrame.Width -= w;
				yield return new CellPos () { Cell = c, Frame = f };
			}
		}

		class CellPos
		{
			public NSCell Cell;
			public CGRect Frame;
		}
	}
}
#endif