// ParameterInformationWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
using System.Text;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Projects.Gui.Completion
{
	class ParameterInformationWindow: Gtk.Window
	{
		Gtk.Label desc;
		Gtk.Label count;
		Gtk.Arrow goPrev;
		Gtk.Arrow goNext;
		HBox mainBox;
		
		public ParameterInformationWindow(): base (WindowType.Popup)
		{
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 2;
			this.TypeHint = Gdk.WindowTypeHint.Dialog;

			desc = new Gtk.Label ("");
			desc.Xalign = 0;
			count = new Gtk.Label ("");
			
			mainBox = new HBox (false, 2);
			mainBox.BorderWidth = 3;
			goPrev = new Gtk.Arrow (Gtk.ArrowType.Up, ShadowType.None);
			mainBox.PackStart (goPrev, false, false, 2);
			mainBox.PackStart (count, false, false, 2);
			goNext = new Gtk.Arrow (Gtk.ArrowType.Down, ShadowType.None);
			mainBox.PackStart (goNext, false, false, 2);
			mainBox.PackStart (desc, true, true, 0);
			mainBox.ShowAll ();
			this.Add (mainBox);
			WindowTransparencyDecorator.Attach (this);
		}
		
		public Gtk.Requisition ShowParameterInfo (IParameterDataProvider provider, int overload, int currentParam)
		{
			int numParams = provider.GetParameterCount (overload);
			
			string[] paramText = new string [numParams];
			for (int n=0; n<numParams; n++) {
				string txt = provider.GetParameterMarkup (overload, n);
				if (n == currentParam)
					txt = "<u><span foreground='darkblue'>" + txt + "</span></u>";
				paramText [n] = txt;
			}
			string text = provider.GetMethodMarkup (overload, paramText);

			desc.Markup = text;
			if (provider.OverloadCount > 1) {
				count.Show ();
				goPrev.Show ();
				goNext.Show ();
				count.Text = GettextCatalog.GetString ("{0} of {1}", overload+1, provider.OverloadCount);
			}
			else {
				count.Hide ();
				goPrev.Hide ();
				goNext.Hide ();
			}
			Gtk.Requisition req = mainBox.SizeRequest ();
			Resize (req.Width, req.Height);
			return req;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			base.OnExposeEvent (args);
			
			int winWidth, winHeight;
			this.GetSize (out winWidth, out winHeight);
			this.GdkWindow.DrawRectangle (this.Style.ForegroundGC (StateType.Insensitive), false, 0, 0, winWidth-1, winHeight-1);
			return false;
		}		
	}
}
