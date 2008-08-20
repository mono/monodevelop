// ExtensionPointsEditorWidget.cs
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
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Components;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.AddinAuthoring
{
	[System.ComponentModel.Category("MonoDevelop.AddinAuthoring")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExtensionPointsEditorWidget : Gtk.Bin
	{
		ListStore store;
		AddinData data;
		AddinDescription adesc;
		TreeViewState state;

		public event EventHandler Changed;
		
		public ExtensionPointsEditorWidget ()
		{
			this.Build();
			
			store = new ListStore (typeof(ExtensionPoint), typeof(string), typeof(string));
			tree.Model = store;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererPixbuf cpix = new CellRendererPixbuf ();
			cpix.Yalign = 0;
			col.PackStart (cpix, false);
			col.AddAttribute (cpix, "stock-id", 1);
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "markup", 2);
			tree.AppendColumn (col);
			
			state = new TreeViewState (tree, 2);
			tree.Selection.Changed += OnSelectionChanged;
		}
		
		void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public void SetData (AddinDescription desc, AddinData data)
		{
			this.data = data;
			this.adesc = desc;
			Fill ();
		}
		
		void Fill ()
		{
			state.Save ();
			
			store.Clear ();
			
			foreach (ExtensionPoint extep in adesc.ExtensionPoints) {
				string name;
				if (extep.Name.Length > 0 && extep.Description.Length > 0) {
					name = "<b>" + GLib.Markup.EscapeText (extep.Name) + "</b>";
					name += "\n<small>" + GLib.Markup.EscapeText (extep.Description) + "</small>";
				}
				else if (extep.Name.Length > 0) {
					name = "<b>" + GLib.Markup.EscapeText (extep.Name) + "</b>";
				}
				else if (extep.Description.Length > 0) {
					name = "<b>" + GLib.Markup.EscapeText (extep.Description) + "</b>";
				}
				else
					name = "<b>" + GLib.Markup.EscapeText (extep.Path) + "</b>";				
				store.AppendValues (extep, "md-extension-point", name);
			}
			
			state.Load ();
			UpdateButtons ();
		}
		
		void UpdateButtons ()
		{
			TreeIter it;
			buttonRemove.Sensitive = buttonProperties.Sensitive = tree.Selection.GetSelected (out it);
		}

		protected virtual void OnButtonNewClicked (object sender, System.EventArgs e)
		{
			ExtensionPoint ep = new ExtensionPoint ();
			NewExtensionPointDialog dlg = new NewExtensionPointDialog ((DotNetProject)data.Project, data.AddinRegistry, adesc, ep);
			try {
				if (dlg.Run () == (int) ResponseType.Ok) {
					adesc.ExtensionPoints.Add (ep);
					Fill ();
					NotifyChanged ();
				}
			} finally {
				dlg.Destroy ();
			}
		}

		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter it;
			tree.Selection.GetSelected (out it);
			ExtensionPoint ep = (ExtensionPoint) store.GetValue (it, 0);
			adesc.ExtensionPoints.Remove (ep);
			store.Remove (ref it);
			NotifyChanged ();
		}

		protected virtual void OnButtonPropertiesClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			tree.Selection.GetSelected (out iter);
			
			ExtensionPoint ep = (ExtensionPoint) store.GetValue (iter, 0);
			ExtensionPoint epc = new ExtensionPoint ();
			epc.CopyFrom (ep);
			NewExtensionPointDialog epdlg = new NewExtensionPointDialog ((DotNetProject)data.Project, data.AddinRegistry, adesc, epc);
			if (epdlg.Run () == (int) ResponseType.Ok)
				ep.CopyFrom (epc);
			epdlg.Destroy ();
			Fill ();
			NotifyChanged ();
		}
		
		void OnSelectionChanged (object s, EventArgs args)
		{
			UpdateButtons ();
		}
	}
}
