// 
// ExtensionView.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Description;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.AddinAuthoring.Gui
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExtensionView : Gtk.Bin
	{
		public ExtensionView ()
		{
			this.Build ();
		}
		
		public void Fill (Extension ext, ITreeNavigator nav)
		{
			labelName.Markup = "<small>Extension</small>\n<big><b>" + GLib.Markup.EscapeText (Util.GetDisplayName (ext)) + "</b></big>";
			object parent = ext.GetExtendedObject ();
			
			if (parent is ExtensionPoint) {
				ExtensionPoint ep = (ExtensionPoint) parent;
				string txt = "<small>Extension Point</small>\n<b>" + GLib.Markup.EscapeText (Util.GetDisplayName (ep)) + "</b>";
				if (!string.IsNullOrEmpty (ep.Description))
					txt += "\n" + GLib.Markup.EscapeText (ep.Description);
				Gtk.Label lab = new Gtk.Label ();
				lab.Xalign = lab.Yalign = 0;
				lab.Markup = txt;
				lab.WidthRequest = 400;
				lab.Wrap = true;
				Gtk.Image img = new Gtk.Image (ImageService.GetPixbuf ("md-extension-point", Gtk.IconSize.Menu));
				img.Yalign = 0;
				Gtk.HBox box = new Gtk.HBox (false, 6);
				box.PackStart (img, false, false, 0);
				box.PackStart (lab, true, true, 0);
				buttonExt.Add (box);
				buttonExt.ShowAll ();
				buttonExt.Clicked += delegate {
					if (nav.MoveToObject (ext)) {
						nav.MoveToParent (typeof(Solution));
						nav.Expanded = true;
						if (nav.MoveToObject (ep.ParentAddinDescription)) {
							nav.Expanded = true;
							if (nav.MoveToObject (ep))
								nav.Selected = true;
						}
					}
				};
			}
		}
	}
}

