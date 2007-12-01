// Navbar.cs
//
// Author:
//   John Luke  <jluke@cfl.rr.com>
//
// Copyright (c) 2004 John Luke
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
using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class Navbar : VBox
	{
		Entry address;

		const string uiInfo = 
            "<toolbar>" +
            "  <toolitem name=\"back\" action=\"back\" />" +
            "  <toolitem name=\"forward\" action=\"forward\" />" +
            "  <toolitem name=\"stop\" action=\"stop\" />" +
            "  <toolitem name=\"reload\" action=\"reload\" />" +
            "  <toolitem name=\"go\" action=\"go\" />" +
            "</toolbar>";

		public Navbar () : this (Gtk.IconSize.SmallToolbar)
		{
		}

		public Navbar (Gtk.IconSize size)
		{
			address = new Entry ("address");
			// FIXME: this doesnt't seem to work yet
			// address.Completion = new EntryCompletion ();
			address.WidthChars = 50;
			address.Activated += new EventHandler (OnGoUrl);

			ActionEntry[] actions = new ActionEntry[]
			{
				new ActionEntry ("back", Gtk.Stock.GoBack, null, null, GettextCatalog.GetString ("Go back"), new EventHandler (OnBackClicked)),
				new ActionEntry ("forward", Gtk.Stock.GoForward, null, null, GettextCatalog.GetString ("Go forward"), new EventHandler (OnForwardClicked)),
				new ActionEntry ("stop", Gtk.Stock.Stop, null, null, GettextCatalog.GetString ("Stop loading"), new EventHandler (OnStopClicked)),
				new ActionEntry ("reload", Gtk.Stock.Refresh, null, null, GettextCatalog.GetString ("Address"), new EventHandler (OnReloadClicked)),
				new ActionEntry ("go", Gtk.Stock.Ok, null, null, GettextCatalog.GetString ("Load address"), new EventHandler (OnGoUrl))
			};

			ActionGroup ag = new ActionGroup ("navbarGroup");
			ag.Add (actions);

			UIManager uim = new UIManager ();
			uim.InsertActionGroup (ag, 0);
			uim.AddWidget += new AddWidgetHandler (OnAddWidget);
			uim.AddUiFromString (uiInfo);

			ToolItem item = new ToolItem ();
			item.Add (address);
	
			Toolbar tb = uim.GetWidget ("/ui/toolbar") as Toolbar;
			tb.IconSize = size;
			tb.Add (item);
			this.ShowAll ();
		}

		void OnAddWidget (object sender, AddWidgetArgs a)
		{
			a.Widget.Show ();
			this.Add (a.Widget);
		}

		public string Url {
			get {
				return address.Text;
			}
			set {
				address.Text = value;
			}
		}

		void OnGoUrl (object o, EventArgs args)
		{
			if (Go != null)
				Go (this, EventArgs.Empty);
		}

		void OnBackClicked (object o, EventArgs args)
		{
			if (Back != null)
				Back (this, EventArgs.Empty);
		}

		void OnForwardClicked (object o, EventArgs args)
		{
			if (Forward != null)
				Forward (this, EventArgs.Empty);
		}

		void OnStopClicked (object o, EventArgs args)
		{
			if (Stop != null)
				Stop (this, EventArgs.Empty);
		}

		void OnReloadClicked (object o, EventArgs args)
		{
			if (Reload != null)
				Reload (this, EventArgs.Empty);
		}

		public event EventHandler Back;
		public event EventHandler Forward;
		public event EventHandler Stop;
		public event EventHandler Reload;
		public event EventHandler Go;
	}
}

