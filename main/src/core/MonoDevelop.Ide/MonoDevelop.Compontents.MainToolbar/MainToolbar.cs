// 
// MainToolbar.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Gtk;
namespace MonoDevelop.Compontents.MainToolbar
{
	public class MainToolbar: Gtk.EventBox
	{
		HBox contentBox = new HBox (false, 6);

		public Cairo.ImageSurface Background {
			get;
			set;
		}

		public int TitleBarHeight {
			get;
			set;
		}

		public Cairo.Color BottomColor {
			get;
			set;
		}

		public MainToolbar ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			contentBox.BorderWidth = 6;
			AddWidget (new Gtk.Button ("Run"));
			AddWidget (new Gtk.ComboBox (new [] {"Debug", "Release"}));
			AddWidget (new Gtk.ComboBox (new [] {"Simulator", "IPhone"}));
			AddWidget (new Gtk.Button ("Stop"));
			AddWidget (new Gtk.Button ("Step over"));
			AddWidget (new Gtk.Button ("Step into"));
			AddWidget (new Gtk.Button ("Step out"));
			AddWidget (new Gtk.ComboBox (new [] {"Dummy"}));
			AddWidget (new Gtk.Entry ());
			Add (contentBox);
		}
		
		public void AddWidget (Gtk.Widget widget)
		{
			contentBox.PackStart (widget, false, false, 0);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Rectangle (
					evnt.Area.X,
					evnt.Area.Y,
					evnt.Area.Width,
					evnt.Area.Height
				);
				context.Clip ();
				context.LineWidth = 1;
				for (int x=0; x < Allocation.Width; x+= Background.Width) {
					Background.Show (context, x, -TitleBarHeight);
				}
				context.MoveTo (0, Allocation.Bottom + 0.5);
				context.LineTo (Allocation.Width + evnt.Area.Width, Allocation.Bottom + 0.5);
				context.Color = BottomColor;
				context.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}
	}
}

