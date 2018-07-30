//
// FlagsSelectorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	public class FlagsSelectorDialog: IDisposable
	{
		Gtk.TreeView treeView;
		Gtk.Dialog dialog;
		Gtk.ListStore store;
		Gtk.Window parent;
		ulong flags;
		Array values;
		
		public FlagsSelectorDialog (Gtk.Window parent, Type enumDesc, ulong flags, string title)
		{
			this.flags = flags;
			this.parent = parent;

			Gtk.ScrolledWindow sc = new Gtk.ScrolledWindow ();
			sc.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sc.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sc.ShadowType = Gtk.ShadowType.In;
			sc.BorderWidth = 6;
			
			treeView = new Gtk.TreeView ();
			sc.Add (treeView);
			
			dialog = new Gtk.Dialog ();
			IdeTheme.ApplyTheme (dialog);
			dialog.ContentArea.Add (sc);
			dialog.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			dialog.AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);
			
			store = new Gtk.ListStore (typeof(bool), typeof(string), typeof(ulong));
			treeView.Model = store;
			treeView.HeadersVisible = false;
			
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			
			Gtk.CellRendererToggle tog = new Gtk.CellRendererToggle ();
			tog.Toggled += new Gtk.ToggledHandler (OnToggled);
			col.PackStart (tog, false);
			col.AddAttribute (tog, "active", 0);
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 1);
			
			treeView.AppendColumn (col);

			values = System.Enum.GetValues (enumDesc);
			foreach (object value in values) {
				ulong val = Convert.ToUInt64 (value);
				store.AppendValues ((flags == 0 && val == 0) || ((flags & val) != 0), value.ToString (), val);
			}
		}
		
		public int Run ()
		{
			dialog.DefaultWidth = 500;
			dialog.DefaultHeight = 400;
			dialog.ShowAll ();
			return MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog, parent);
		}
		
		public void Dispose ()
		{
			dialog.Dispose ();
		}
		
		void OnToggled (object s, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;
			
			bool oldValue = (bool) store.GetValue (iter, 0);
			ulong flag = (ulong) store.GetValue (iter, 2);

			if (oldValue)
				flags &= ~flag;
			else {
				if (flag == 0)
					flags = 0;
				else
					flags |= flag;
			}
			
			store.GetIterFirst (out iter);
			foreach (object value in values) {
				ulong val = Convert.ToUInt64 (value);
				store.SetValue (iter, 0, (flags == 0 && val == 0) || ((flags & val) != 0));
				store.IterNext (ref iter);
			}
		}

		public ulong Value {
			get { return flags; }
		}
	}
}
