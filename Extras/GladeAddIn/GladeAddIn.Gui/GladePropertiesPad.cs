//
// GladePropertiesPad.cs
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
	public class GladePropertiesPad: AbstractPadContent
	{
		Gtk.Widget widget;
		Button addButton;
		
		public GladePropertiesPad (): base ("")
		{
			DefaultPlacement = "GladeAddIn.Gui.GladeWidgetTreePad/right; bottom";
			VBox box = new VBox ();
			CheckButton la = new CheckButton ("Map to code");
		//	box.PackStart (la, false, false, 6);
			box.PackStart (GladeService.App.Editor, true, true, 0);
			widget = box;
			
			Pango.FontDescription fd = widget.Style.FontDesc.Copy ();
			Console.WriteLine ("fd.Size:" + fd.Size);
			fd.Size /= 2;
			Console.WriteLine ("fd.Size:" + fd.Size);
			la.ModifyFont (Pango.FontDescription.FromString ("Arial 4pt"));
			la.ModifyBg (StateType.Normal, new Gdk.Color (255,0,0));
			
			HButtonBox bbox = GladeService.App.Editor.Children [1] as HButtonBox;
			Button rbut = bbox.Children [0] as Button;
			addButton = new Button ("Add to class");
			addButton.BorderWidth = rbut.BorderWidth;
			bbox.PackStart (addButton, false, true, 0);
			addButton.Clicked += new EventHandler (OnAdd);
			
			widget.ShowAll ();
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
		
		void OnAdd (object s, EventArgs a)
		{
			GladeService.AddCurrentWidgetToClass ();
		}
	}
}
