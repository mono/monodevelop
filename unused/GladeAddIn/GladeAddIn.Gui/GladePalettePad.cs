//
// GladePalettePad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Ide.Gui;

namespace GladeAddIn.Gui
{
	public class GladePalettePad: AbstractPadContent
	{
		Gtk.Widget widget;
		
		public GladePalettePad (): base ("")
		{
			DefaultPlacement = "right";
			try {
				widget = GladeService.App.Palette;
				widget.ShowAll ();
				HideWindowWidgets (widget);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
		
		void HideWindowWidgets (Gtk.Widget w)
		{
			if (w is Gtk.RadioButton) {
				IntPtr data = w.GetData ("user");
				if (!data.Equals (IntPtr.Zero)) {
					Gladeui.WidgetClass wc = new Gladeui.WidgetClass (data);
					Type t = (Type) wc.Type;
					if (typeof(Gtk.Window).IsAssignableFrom (t))
						w.Hide ();
				}
			} else if (w is Gtk.Container) {
				foreach (Widget cw in ((Container)w).Children)
					HideWindowWidgets (cw);
			}
		}
	}
}
