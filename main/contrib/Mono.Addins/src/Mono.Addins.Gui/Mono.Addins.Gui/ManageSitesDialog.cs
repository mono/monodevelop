//
// ManageSitesDialog.cs
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
using Gtk;
using Mono.Unix;

using Mono.Addins.Setup;


namespace Mono.Addins.Gui
{
	partial class ManageSitesDialog : Dialog
	{
		ListStore treeStore;
		SetupService service;
		
		public ManageSitesDialog (SetupService service)
		{
			Build ();
			this.service = service;
			treeStore = new Gtk.ListStore (typeof (string), typeof (string));
			repoTree.Model = treeStore;
			repoTree.HeadersVisible = true;
			repoTree.AppendColumn (Catalog.GetString ("Name"), new Gtk.CellRendererText (), "text", 1);
			repoTree.AppendColumn (Catalog.GetString ("Url"), new Gtk.CellRendererText (), "text", 0);
			repoTree.Selection.Changed += new EventHandler(OnSelect);
			
			AddinRepository[] reps = service.Repositories.GetRepositories ();
			foreach (AddinRepository rep in reps) {
				treeStore.AppendValues (rep.Url, rep.Title);
			}

			btnRemove.Sensitive = false;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
		
		protected void OnAdd (object sender, EventArgs e)
		{
			NewSiteDialog dlg = new NewSiteDialog ();
			try {
				if (dlg.Run ()) {
					string url = dlg.Url;
					if (!url.StartsWith ("http://") && !url.StartsWith ("https://") && !url.StartsWith ("file://")) {
						url = "http://" + url;
					}
					
					try {
						new Uri (url);
					} catch {
						Services.ShowError (null, "Invalid url: " + url, null, true);
					}
					
					if (!service.Repositories.ContainsRepository (url)) {
						IProgressStatus m = new ConsoleProgressStatus (false);
						AddinRepository rr = service.Repositories.RegisterRepository (m, url);
						if (rr == null) {
							Services.ShowError (null, "The repository could not be registered", null, true);
							return;
						}
						treeStore.AppendValues (rr.Url, rr.Title);
					}
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected void OnRemove (object sender, EventArgs e)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!repoTree.Selection.GetSelected (out foo, out iter))
				return;
				
			string rep = (string) treeStore.GetValue (iter, 0);
			service.Repositories.RemoveRepository (rep);
			
			treeStore.Remove (ref iter);
		}

		protected void OnSelect(object sender, EventArgs e)
		{
			btnRemove.Sensitive = repoTree.Selection.CountSelectedRows() > 0;
		}
	}
}
