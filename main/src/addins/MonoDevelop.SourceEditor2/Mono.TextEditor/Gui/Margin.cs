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
using System.Collections.Generic;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;

using MonoDevelop.Components.AtkCocoaHelper;

namespace Mono.TextEditor
{
	abstract class Margin : IDisposable
	{
		public abstract double Width {
			get;
		}

		bool isVisible;
		public bool IsVisible {
			get {
				return isVisible;
			}
			set {
				isVisible = value;

				if (Accessible != null) {
					Accessible.Hidden = !value;
				}
			}
		}
		
		// set by the text editor
		public virtual double XOffset {
			get;
			internal set;
		}
		
		protected Gdk.Cursor cursor = null;
		
		public Gdk.Cursor MarginCursor {
			get {
				return cursor;
			}
		}
		
		List<MarginDrawer> marginDrawer = new List<MarginDrawer> ();
		public IEnumerable<MarginDrawer> MarginDrawer {
			get {
				return marginDrawer;
			}
		}

		public IBackgroundRenderer BackgroundRenderer {
			get;
			set;
		}

		AccessibilityElementProxy accessible;
		public virtual AccessibilityElementProxy Accessible {
			get {
				if (accessible == null && AccessibilityElementProxy.Enabled) {
					accessible = new AccessibilityElementProxy ();
				}
				return accessible;
			}
		}

		Gdk.Rectangle rectInParent;
		public Gdk.Rectangle RectInParent {
			get {
				return rectInParent;
			}

			set {
				rectInParent = value;

				if (Accessible == null) {
					return;
				}

				Accessible.FrameInGtkParent = rectInParent;
				// SetFrameInParent is in Cocoa coords, but because margins take up the whole vertical height
				// we don't need to switch anything around and can just pass in the rectInParent
				Accessible.FrameInParent = rectInParent;
			}
		}

		protected Margin ()
		{
			var a = Accessible;
			if (a != null) {
				a.SetRole (AtkCocoa.Roles.AXRuler);
			}
			IsVisible = true;
		}
		
		public void AddDrawer (MarginDrawer drawer)
		{
			marginDrawer.Add (drawer);
		}
		
		public void RemoveDrawer (MarginDrawer drawer)
		{
			marginDrawer.Remove (drawer);
		}
		
		internal protected abstract void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight);
		
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
	
	class MarginMouseEventArgs : EventArgs
	{
		public double X {
			get;
			private set;
		}
		
		public double Y {
			get;
			private set;
		}
		
		/// <summary>
		/// The raw GDK event. May be null if the event was synthesized.
		/// </summary>
		public Gdk.Event RawEvent { get; private set; }
		
		public Gdk.EventType Type {
			get;
			private set;
		}
		
		public Gdk.ModifierType ModifierState {
			get;
			private set;
		}
		
		public uint Button {
			get;
			private set;
		}
		
		public bool TriggersContextMenu ()
		{
			var evt = RawEvent as Gdk.EventButton;
			return evt != null && evt.TriggersContextMenu ();
		}
		
		int lineNumber = -2; // -2 means that line number has not yet been calculated
		public int LineNumber {
			get {
				if (lineNumber == -2) {
					try {
						lineNumber = Editor.YToLine (Editor.VAdjustment.Value + Y);
						if (lineNumber > Editor.Document.LineCount)
							lineNumber = 0;
					} catch (Exception) {
						lineNumber = 0;
					}
				}
				return lineNumber;
			}
		}
		
		DocumentLine line;
		public DocumentLine LineSegment {
			get {
				if (line == null) {
					if (LineNumber < DocumentLocation.MinLine)
						return null;
					line = Editor.Document.GetLine (lineNumber);
				}
				return line;
			}
		}

		public MonoTextEditor Editor {
			get;
		}
		
		public MarginMouseEventArgs (MonoTextEditor editor, Gdk.Event raw, uint button, double x, double y, Gdk.ModifierType modifierState)
			: this (editor, raw.Type, button, x, y, modifierState)
		{
			this.RawEvent = raw;
		}
		
		public MarginMouseEventArgs (MonoTextEditor editor, Gdk.EventType type, uint button, double x, double y, Gdk.ModifierType modifierState)
		{
			this.Editor = editor;
			this.Type = type;
			
			this.Button = button;
			this.X = x;
			this.Y = y;
			this.ModifierState = modifierState;
		}
	}
}