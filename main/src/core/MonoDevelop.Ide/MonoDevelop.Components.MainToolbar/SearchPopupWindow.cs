//
// SearchPopupWindow.cs
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
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	public class SearchPopupWindow : Gtk.Window
	{
		SearchPopupWidget widget;

		public SearchPopupWindow () : base(WindowType.Popup)
		{
			widget = new SearchPopupWidget (this);
			TransientFor = IdeApp.Workbench.RootWindow;
			SkipTaskbarHint = true;
			SkipPagerHint = true;
			Add (widget);
			widget.SizeRequested += delegate(object o, SizeRequestedArgs args) {
				Resize (args.Requisition.Width, args.Requisition.Height);
			};
			widget.ItemActivated += (sender, e) => OpenFile ();
		}

		public void Update (string searchPattern)
		{
			widget.Update (searchPattern);
		}

		internal void OpenFile ()
		{
			var region = widget.SelectedItemRegion;
			if (string.IsNullOrEmpty (region.FileName))
				return;
			if (region.Begin.IsEmpty) {
				IdeApp.Workbench.OpenDocument (region.FileName);
			} else {
				IdeApp.Workbench.OpenDocument (region.FileName, region.BeginLine, region.BeginColumn);
			}
			Destroy ();
		}

		public bool ProcessKey (Gdk.Key key)
		{
			return widget.ProcessKey (key);
		}
	}
}

