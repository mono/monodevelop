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
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Completion
{
	class ParameterInformationWindow: TooltipWindow
	{
		Gtk.Label desc;
		Gtk.Label count;
		Gtk.Arrow goPrev;
		Gtk.Arrow goNext;
		HBox mainBox;
		
		public ParameterInformationWindow ()
		{
			desc = new Gtk.Label ("");
			desc.Xalign = 0;
			count = new Gtk.Label ("");

			mainBox = new HBox (false, 2);
			mainBox.BorderWidth = 3;

			HBox arrowHBox = new HBox ();

			goPrev = new Gtk.Arrow (Gtk.ArrowType.Up, ShadowType.None);
			arrowHBox.PackStart (goPrev, false, false, 0);
			arrowHBox.PackStart (count, false, false, 0);
			goNext = new Gtk.Arrow (Gtk.ArrowType.Down, ShadowType.None);
			arrowHBox.PackStart (goNext, false, false, 0);

			VBox vBox = new VBox ();
			vBox.PackStart (arrowHBox, false, false, 0);
			
			mainBox.PackStart (vBox, false, false, 0);
			mainBox.PackStart (desc, true, true, 0);
			mainBox.ShowAll ();
			this.Add (mainBox);
			
			EnableTransparencyControl = true;
		}
		
		public Gtk.Requisition ShowParameterInfo (IParameterDataProvider provider, int overload, int currentParam)
		{
			if (provider == null)
				throw new ArgumentNullException ("provider");
			int numParams = System.Math.Max (0, provider.GetParameterCount (overload));
			
			string[] paramText = new string[numParams];
			for (int i = 0; i < numParams; i++) {
				string txt = provider.GetParameterMarkup (overload, i);
				if (i == currentParam)
					txt = "<u><span foreground='darkblue'>" + txt + "</span></u>";
				paramText [i] = txt;
			}
			string text = provider.GetMethodMarkup (overload, paramText, currentParam);

			desc.Markup = text;
			if (provider.OverloadCount > 1) {
				count.Show ();
				goPrev.Show ();
				goNext.Show ();
				count.Text = GettextCatalog.GetString ("{0} of {1}", overload+1, provider.OverloadCount);
			} else {
				count.Hide ();
				goPrev.Hide ();
				goNext.Hide ();
			}
			Gtk.Requisition req = mainBox.SizeRequest ();
			Resize (req.Width, req.Height);
			return req;
		}		
	}
}
