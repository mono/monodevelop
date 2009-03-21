// Margin.cs
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
	public abstract class Margin : IDisposable
	{
		public abstract int Width {
			get;
		}
		
		public bool IsVisible { get; set; }
		
		// set by the text editor
		public int XOffset {
			get;
			internal set;
		}
		
		protected Gdk.Cursor cursor = null;
		
		public Gdk.Cursor MarginCursor {
			get {
				return cursor;
			}
		}
		
		protected Margin ()
		{
			IsVisible = true;
		}
		
		internal protected abstract void Draw (Gdk.Drawable drawable, Gdk.Rectangle area, int line, int x, int y);
		
		internal protected virtual void OptionsChanged ()
		{
		}
		
		internal protected virtual void MousePressed (MarginMouseEventArgs args)
		{
			if (ButtonPressed != null)
				ButtonPressed (this, args);
		}
		
		internal protected virtual void MouseReleased (MarginMouseEventArgs args)
		{
			if (ButtonReleased != null)
				ButtonReleased (this, args);
		}
		
		internal protected virtual void MouseHover (MarginMouseEventArgs args)
		{
			if (MouseMoved != null)
				MouseMoved (this, args);
		}
		
		internal protected virtual void MouseLeft ()
		{
			if (MouseLeave != null)
				MouseLeave (this, EventArgs.Empty);
		}
		
		public virtual void Dispose ()
		{
			cursor = cursor.Kill ();
		}
		
		public event EventHandler<MarginMouseEventArgs> ButtonPressed;
		public event EventHandler<MarginMouseEventArgs> ButtonReleased;
		public event EventHandler<MarginMouseEventArgs> MouseMoved;
		public event EventHandler MouseLeave;
	}
	
	public class MarginMouseEventArgs : EventArgs
	{
		int button;
		int x;
		int y;
		Gdk.EventType type;
		Gdk.ModifierType modifierState;
		TextEditor editor;
		
		LineSegment line;
		int lineNumber = -2; // -2 means that line number has not yet been calculated
		
		public MarginMouseEventArgs (TextEditor editor, int button, int x, int y, Gdk.EventType type, Gdk.ModifierType modifierState)
		{
			this.editor = editor;
			this.button = button;
			this.x = x;
			this.y = y;
			this.type = type;
			this.modifierState = modifierState;
		}
		
		public int Y {
			get {
				return y;
			}
		}
		
		public int X {
			get {
				return x;
			}
		}
		
		public Gdk.EventType Type {
			get {
				return type;
			}
		}
		
		public Gdk.ModifierType ModifierState {
			get {
				return modifierState;
			}
		}
		
		public int Button {
			get {
				return button;
			}
		}
		
		public int LineNumber {
			get {
				if (lineNumber == -2) {
					lineNumber = Editor.Document.VisualToLogicalLine ((int)((y + editor.VAdjustment.Value) / editor.LineHeight));
					if (lineNumber >= editor.Document.LineCount)
						lineNumber = -1;
				}
				return lineNumber;
			}
		}

		public LineSegment LineSegment {
			get {
				if (line == null) {
					if (LineNumber == -1)
						return null;
					line = editor.Document.GetLine (lineNumber);
				}
				return line;
			}
		}

		public TextEditor Editor {
			get {
				return editor;
			}
		}
	}
}
