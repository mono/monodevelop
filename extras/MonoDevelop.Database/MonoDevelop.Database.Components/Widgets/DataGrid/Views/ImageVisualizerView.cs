//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2008 Ben Motmans
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

using Gtk;
using System;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Database.Components
{
	public class ImageVisualizerView : AbstractViewContent
	{
		private VBox vbox;
		private ScrolledWindow scrolledWindow;
		private Gtk.Image image;
		
		public ImageVisualizerView ()
		{
			vbox = new VBox (false, 6);
			vbox.BorderWidth = 6;
			
			image = new Gtk.Image ();

			scrolledWindow = new ScrolledWindow ();
			scrolledWindow.Add (image);
			
			vbox.PackStart (scrolledWindow, true, true, 0);
			
			vbox.ShowAll ();
		}

		public override string UntitledName {
			get { return AddinCatalog.GetString ("Image"); }
		}
		
		public override void Dispose ()
		{
			Control.Destroy ();
		}
		
		public override void Load (string filename)
		{
			throw new NotSupportedException ();
		}
		
		public override Widget Control {
			get { return vbox; }
		}
		
		public void Load (object dataObject)
		{
			byte[] blob = dataObject as byte[];
			if (blob != null) {
				try {
					Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (blob);
					image.Pixbuf = pixbuf;
					return;
				} catch {}
			}
			
			image.Pixbuf = PixbufService.GetPixbuf ("gtk-missing-image", IconSize.Dialog);
		}
	}
}
