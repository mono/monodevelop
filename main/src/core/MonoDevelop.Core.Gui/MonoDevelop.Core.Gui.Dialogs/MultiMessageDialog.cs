// MultiMessageDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public partial class MultiMessageDialog : Gtk.Dialog
	{
		Image lastImage;
		
		public MultiMessageDialog()
		{
			this.Build();
		}

		public void AddWarning (string msg)
		{
			AddMessage (msg, Gtk.Stock.DialogWarning);
		}

		public void AddError (string msg)
		{
			AddMessage (msg, Gtk.Stock.DialogError);
		}
		
		public void AddMessage (string msg, string icon)
		{
			if (lastImage != null) {
				HSeparator sep = new HSeparator ();
				sep.Show ();
				msgBox.PackStart (sep);
				lastImage.IconSize = (int) Gtk.IconSize.Button;
			}
			
			HBox box = new HBox ();
			box.Spacing = 12;
			Alignment imgBox = new Alignment (0, 0, 0, 0);
			Image img = new Image (icon, lastImage != null ? Gtk.IconSize.Button : IconSize.Dialog);
			imgBox.Add (img);
			lastImage = img;
			box.PackStart (imgBox, false, false, 0);
			Label lab = new Label (msg);
			lab.Xalign = 0;
			lab.Yalign = 0;
			lab.Wrap = true;
			lab.WidthRequest = 500;
			box.PackStart (lab, true, true, 0);
			msgBox.PackStart (box, false, false, 0);
			box.ShowAll ();
		}

		public new int Run ()
		{
			if (msgBox.SizeRequest ().Height < 400) {
				scrolled.VscrollbarPolicy = PolicyType.Never;
				this.Resize (10, 10);
			}
			return base.Run ();
		}
	}
}
