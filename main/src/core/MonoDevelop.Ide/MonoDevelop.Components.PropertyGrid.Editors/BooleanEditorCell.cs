//
// BooleanEditorCell.cs
//
// Author:
//   Lluis Sanchez Gual
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
using System.ComponentModel;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	[PropertyEditorType (typeof (bool))]
	public class BooleanEditorCell : PropertyEditorCell 
	{
		static int indicatorSize;
		static int indicatorSpacing;
		static Gtk.Style style;

		static BooleanEditorCell ()
		{
			// reinit style
			MonoDevelop.Ide.Gui.Styles.Changed += (sender, e) => style = null;
		}

		// we can't override Initialize () or use the default constructor for this,
		// because a valid Gdk.Window is required for full Gtk.Style initialization
		static void InitializeStyle (Gtk.Widget container)
		{
			if (style == null && container.GdkWindow != null) {
				Gtk.CheckButton cb = new BooleanEditor ();
				cb.GdkWindow = container.GdkWindow;
				cb.Parent = container;
				cb.Realize ();
				style = cb.Style;
				style.Attach (container.GdkWindow);
				indicatorSize = (int) cb.StyleGetProperty ("indicator-size");
				indicatorSpacing = (int) cb.StyleGetProperty ("indicator-spacing");
				style.Detach ();
				cb.Dispose ();
			}
		}
		
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			width = indicatorSize;
			height = indicatorSize;
		}
		
		public override void Render (Gdk.Drawable window, Cairo.Context ctx, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			if (style == null)
				InitializeStyle (Container);
			
			Gtk.ShadowType sh = (bool) Value ? Gtk.ShadowType.In : Gtk.ShadowType.Out;

			int s = indicatorSize - 1;
			if (s > bounds.Height)
				s = bounds.Height;
			if (s > bounds.Width)
				s = bounds.Width;

			Gtk.Style.PaintCheck (style, window, state, sh, bounds, Container, "checkbutton", bounds.X + indicatorSpacing - 1, bounds.Y + (bounds.Height - s)/2, s, s);
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new BooleanEditor { State = state };
		}
	}
	
	public class BooleanEditor : Gtk.CheckButton, IPropertyEditor 
	{
		public void Initialize (EditSession session)
		{
			if (session.Property.PropertyType != typeof(bool))
				throw new ApplicationException ("Boolean editor does not support editing values of type " + session.Property.PropertyType);
			Sensitive = !session.Property.IsReadOnly;
		}
		
		public object Value { 
			get { return Active; } 
			set { Active = (bool) value; }
		}
		
		protected override void OnToggled ()
		{
			base.OnToggled ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
