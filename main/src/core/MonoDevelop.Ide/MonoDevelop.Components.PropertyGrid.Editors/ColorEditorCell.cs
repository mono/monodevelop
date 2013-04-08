//
// BooleanEditorCell.cs
//
// Author:
//   Lluis Sanchez Gual
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
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
	[PropertyEditorType (typeof (System.Drawing.Color))]
	public class ColorEditorCell: PropertyEditorCell 
	{
		const int ColorBoxSize = 16;
		const int ColorBoxSpacing = 3;
		
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			base.GetSize (availableWidth - ColorBoxSize - ColorBoxSpacing, out width, out height);
			width += ColorBoxSize + ColorBoxSpacing;
			if (height < ColorBoxSize) height = ColorBoxSize;
		}
		
		protected override string GetValueText ()
		{
			System.Drawing.Color color = (System.Drawing.Color) Value;
			//TODO: dropdown known color selector so this does something
			if (color.IsKnownColor)
				return color.Name;
			else if (color.IsEmpty)
				return "";
			else
				return String.Format("#{0:x2}{1:x2}{2:x2}", color.R, color.G, color.B);
		}
		
		public override void Render (Gdk.Drawable window, Cairo.Context ctx, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			Gdk.GC gc = new Gdk.GC (window);
	   		gc.RgbFgColor = GetColor ();
	   		int yd = (bounds.Height - ColorBoxSize) / 2;
			window.DrawRectangle (gc, true, bounds.X, bounds.Y + yd, ColorBoxSize - 1, ColorBoxSize - 1);
			window.DrawRectangle (Container.Style.BlackGC, false, bounds.X, bounds.Y + yd, ColorBoxSize - 1, ColorBoxSize - 1);
			bounds.X += ColorBoxSize + ColorBoxSpacing;
			bounds.Width -= ColorBoxSize + ColorBoxSpacing;
			base.Render (window, ctx, bounds, state);
		}
		
		private Gdk.Color GetColor ()
		{
			System.Drawing.Color color = (System.Drawing.Color) Value;
			//TODO: Property.Converter.ConvertTo() fails: why?
			return new Gdk.Color (color.R, color.G, color.B);
		}

		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new ColorEditor ();
		}
	}
	
	public class ColorEditor : Gtk.ColorButton, IPropertyEditor
	{
		public void Initialize (EditSession session)
		{
			if (session.Property.PropertyType != typeof(System.Drawing.Color))
				throw new ApplicationException ("Color editor does not support editing values of type " + session.Property.PropertyType);
		}
		
		public object Value { 
			get {
				int red = (int) (255 * (float) Color.Red / ushort.MaxValue);
				int green = (int) (255 * (float) Color.Green / ushort.MaxValue);
				int blue = (int) (255 * (float) Color.Blue / ushort.MaxValue);
				return System.Drawing.Color.FromArgb (red, green, blue);
			}
			set {
				System.Drawing.Color color = (System.Drawing.Color) value;
				Color = new Gdk.Color (color.R, color.G, color.B);
			}
		}
		
		protected override void OnColorSet ()
		{
			base.OnColorSet ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		public event EventHandler ValueChanged;
	}
}
