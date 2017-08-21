//
// DockFrameTopLevel.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using Gdk;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.Docking
{
	class DockFrameTopLevel
	{
		IDockFrameTopLevelControl control;
		internal IDockFrameTopLevelControl Control {
			get {
				return control;
			}
		}

		public DockFrameTopLevel (DockFrame frame)
		{
			control = new DockFrameTopLevelControl ();
			control.Initialize (this, frame);
		}

		protected DockFrameTopLevel (DockFrame frame, IDockFrameTopLevelControl control)
		{
			this.control = control;
			control.Initialize (this, frame); 
		}

		public int X {
			get { return control.X; }
			set {
				control.X = value;
			}
		}
		
		public int Y {
			get { return control.Y; }
			set {
				control.Y = value;
			}
		}

		public Gdk.Size Size {
			get {
				return control.Size;
			}
			set {
				control.Size = value;
			}
		}

		public int Width {
			get {
				return control.Width;
			}
			set {
				control.Width = value;
			}
		}

		public int Height {
			get {
				return control.Height;
			}
			set {
				control.Height = value;
			}
		}

		public bool HasToplevelFocus {
			get {
				return control.HasToplevelFocus;
			}
		}

		public void Present ()
		{
			/*

				*/
			control.Present ();
		}

		public event EventHandler<KeyEventArgs> KeyPressed;
		public event EventHandler<EventArgs> Hidden;

		internal void OnKeyEvent (KeyEventArgs args)
		{
			KeyPressed?.Invoke (this, args);
		}

		internal void OnHidden ()
		{
			Hidden?.Invoke (this, EventArgs.Empty);
		}
	}

	internal class KeyEventArgs
	{
		// TODO: We will need a non-UI toolkit specific representation for keys and modifiers
		public Gdk.Key Key { get; private set; }
		public Gdk.ModifierType Modifiers { get; private set; }

		public KeyEventArgs (Gdk.Key key, Gdk.ModifierType modifiers)
		{
			Key = key;
			Modifiers = modifiers;
		}
	}

	internal interface IDockFrameTopLevelControl
	{
		void Initialize (DockFrameTopLevel parentToplevel, DockFrame frame);
		Gdk.Size Size { get; set; }
		int X { get; set; }
		int Y { get; set; }
		int Width { get; set; }
		int Height { get; set; }
		string Title { get; set; }
		bool HasToplevelFocus { get; }
		void Present ();
	}

	class DockFrameTopLevelControl : EventBox, IDockFrameTopLevelControl
	{
		bool repositionRequested;
		int x, y, width, height;
		DockFrame frame;
		DockFrameTopLevel toplevel;

		public void Initialize (DockFrameTopLevel parentToplevel, DockFrame frame)
		{
			this.frame = frame;
			toplevel = parentToplevel;
		}

		public int X {
			get { return x; }
			set {
				x = value;
				UpdateWindowPosition ();
			}
		}

		public int Y {
			get { return y; }
			set {
				y = value;
				UpdateWindowPosition ();
			}
		}

		public Gdk.Size Size {
			get {
				if (ContainerWindow != null) {
					int w, h;
					ContainerWindow.GetSize (out w, out h);
					return new Gdk.Size (w, h);
				} else {
					return new Gdk.Size (WidthRequest, HeightRequest);
				}
			}
			set {
				width = value.Width;
				height = value.Height;
				if (ContainerWindow != null)
					UpdateWindowPosition ();
				else {
					WidthRequest = value.Width;
					HeightRequest = value.Height;
				}
			}
		}

		public int Width {
			get {
				if (ContainerWindow != null) {
					int w, h;
					ContainerWindow.GetSize (out w, out h);
					return w;
				} else
					return WidthRequest;
			}
			set {
				width = value;
				if (ContainerWindow != null)
					UpdateWindowPosition ();
				else
					WidthRequest = value;
			}
		}

		public int Height {
			get {
				if (ContainerWindow != null) {
					int w, h;
					ContainerWindow.GetSize (out w, out h);
					return h;
				} else
					return HeightRequest;
			}
			set {
				height = value;
				if (ContainerWindow != null)
					UpdateWindowPosition ();
				else
					HeightRequest = value;
			}
		}

		void UpdateWindowPosition ()
		{
			if (ContainerWindow != null) {
				if (!repositionRequested && width != 0 && height != 0) {
					repositionRequested = true;
					Application.Invoke ((o, args) => {
						var widgetControl = frame.Control as Widget;
						if (widgetControl == null) {
							throw new ToolkitMismatchException ();
						}
						var pos = widgetControl.GetScreenCoordinates (new Gdk.Point (x, y));
						DesktopService.PlaceWindow (ContainerWindow, pos.X, pos.Y, width, height);
						repositionRequested = false;
					});
				}
			} else if (Parent != null)
				Parent.QueueResize ();
		}

		internal Gtk.Window ContainerWindow { get; set; }

		public string Title {
			get {
				return ContainerWindow?.Title;
			}
			set {
				if (ContainerWindow != null)
					ContainerWindow.Title = value;
			}
		}

		public bool HasToplevelFocus {
			get {
				return ((Gtk.Window)Toplevel).HasToplevelFocus;
			}
		}

		public void Present ()
		{
			if (ContainerWindow != null && ContainerWindow != (Gtk.Window)Toplevel)
				ContainerWindow.Present ();
		}

        protected override bool OnKeyPressEvent(EventKey evnt)
        {
			var args = new KeyEventArgs (evnt.Key, evnt.State);
			toplevel.OnKeyEvent (args);

			return base.OnKeyPressEvent(evnt);
        }

        protected override void OnHidden()
        {
			toplevel.OnHidden ();

			base.OnHidden();
        }
    }
}
