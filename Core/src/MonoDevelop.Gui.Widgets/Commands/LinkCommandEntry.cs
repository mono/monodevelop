//
// LinkCommandEntry.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using MonoDevelop.Services;

namespace MonoDevelop.Commands
{
	public class LinkCommandEntry: CommandEntry
	{
		static object Id = new object ();
		string text;
		string url;
		string icon = Gtk.Stock.JumpTo;
		
		public LinkCommandEntry (string text, string url): base (Id)
		{
			this.text = text;
			this.url = url;
		}
		
		public LinkCommandEntry (string text, string url, string icon): base (Id)
		{
			this.text = text;
			this.url = url;
			this.icon = icon;
		}
		
		public string Text {
			get { return text; }
			set { text = value; }
		}
		
		public string Url {
			get { return url; }
			set { url = value; }
		}
		
		internal void HandleActivation (object sender, EventArgs e)
		{
			try {
				Gnome.Url.Show (url);
			} catch (Exception) {
				string msg = String.Format (GettextCatalog.GetString ("Could not open the url {0}"), url);
				Gtk.MessageDialog md = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, msg);
				md.Run ();
				md.Hide ();
			}
		}
		
		internal protected override Gtk.MenuItem CreateMenuItem (CommandManager manager)
		{
			Gtk.ImageMenuItem item = new Gtk.ImageMenuItem (text != null ? text : url);
			item.Image = new Gtk.Image (icon, Gtk.IconSize.Menu);
			item.Activated += new EventHandler (HandleActivation);
			return item;
		}

		internal protected override Gtk.ToolItem CreateToolItem (CommandManager manager)
		{
			Gtk.ToolButton item = new Gtk.ToolButton (text);
			item.StockId = icon;
			item.Clicked += new EventHandler (HandleActivation);
			return item;
		}
	}
}

