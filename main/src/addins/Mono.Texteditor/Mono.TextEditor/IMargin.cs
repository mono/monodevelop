// IMargin.cs
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

namespace Mono.TextEditor
{
	public interface IMargin
	{
		bool IsVisible {
			get;
			set;
		}
		int Width {
			get;
		}
		
		int XOffset {
			get;
			set;
		}
		
		Gdk.Cursor MarginCursor {
			get;
		}
		
		void OptionsChanged ();
		
		void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, int line, int x, int y);
		
		void MousePressed (int button, int x, int y, Gdk.EventType type, Gdk.ModifierType modifierState);
		void MouseReleased (int button, int x, int y, Gdk.ModifierType modifierState);
		void MouseHover (int x, int y, bool buttonPressed);
		void MouseLeft ();
	}
	
	public abstract class AbstractMargin : IMargin, IDisposable
	{
		public abstract int Width {
			get;
		}
		bool isVisible = true;
		public bool IsVisible {
			get {
				return isVisible;
			}
			set {
				isVisible = value;
			}
		}
		
		int xOffset = 0;
		// set by the text editor
		public int XOffset {
			get {
				return xOffset;
			}
			set {
				 xOffset = value;
			}
		}
		
		protected Gdk.Cursor cursor = null;
		public Gdk.Cursor MarginCursor {
			get {
				return cursor;
			}
		}
		
		public abstract void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, int line, int x, int y);
		
		public virtual void OptionsChanged ()
		{
		}
		
		public virtual void MousePressed (int button, int x, int y, Gdk.EventType type, Gdk.ModifierType modifierState)
		{
		}
		public virtual void MouseReleased (int button, int x, int y, Gdk.ModifierType modifierState)
		{
		}
		
		public virtual void MouseHover (int x, int y, bool buttonPressed)
		{
		}
		
		public virtual void MouseLeft ()
		{
		}
		
		public virtual void Dispose ()
		{
			if (cursor != null) {
				cursor.Dispose ();
				cursor = null;
			}
		}
		
	}
}
