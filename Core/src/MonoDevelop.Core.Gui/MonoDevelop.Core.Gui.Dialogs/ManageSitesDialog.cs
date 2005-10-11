
using System;
using Gtk;
using Glade;

using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.AddIns.Setup;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public class ManageSitesDialog : IDisposable
	{
		[Glade.Widget ("ManageSitesDialog")] Dialog dialog;
		[Glade.Widget] Gtk.TreeView repoTree;
		ListStore treeStore;
		
		public ManageSitesDialog ()
		{
			new Glade.XML (null, "Base.glade", "ManageSitesDialog", null).Autoconnect (this);
			treeStore = new Gtk.ListStore (typeof (string), typeof (string));
			repoTree.Model = treeStore;
			repoTree.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
			
			RepositoryRecord[] reps = Runtime.SetupService.GetRepositories ();
			foreach (RepositoryRecord rep in reps) {
				treeStore.AppendValues (rep.Url, rep.Title);
			}
		}
		
		public void Show ()
		{
			dialog.ShowAll ();
		}
		
		public void Run ()
		{
			dialog.ShowAll ();
			dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
			dialog.Dispose ();
		}
		
		protected void OnAdd (object sender, EventArgs e)
		{
			using (NewSiteDialog dlg = new NewSiteDialog ()) {
				if (dlg.Run ()) {
					string url = dlg.Url;
					if (!url.StartsWith ("http://") && !url.StartsWith ("https://") && !url.StartsWith ("file://")) {
						url = "http://" + url;
					}
					
					try {
						new Uri (url);
					} catch {
						Services.MessageService.ShowError (null, "Invalid url: " + url, null, true);
					}
					
					if (!Runtime.SetupService.IsRepositoryRegistered (url)) {
						using (IProgressMonitor m = new ConsoleProgressMonitor ()) {
							RepositoryRecord rr = Runtime.SetupService.RegisterRepository (m, url);
							if (rr == null) {
								Services.MessageService.ShowError (null, "The repository could not be registered", null, true);
								return;
							}
							treeStore.AppendValues (rr.Url, rr.Title);
						}
					}
				}
			}
		}
		
		protected void OnRemove (object sender, EventArgs e)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!repoTree.Selection.GetSelected (out foo, out iter))
				return;
				
			string rep = (string) treeStore.GetValue (iter, 0);
			Runtime.SetupService.UnregisterRepository (rep);
			
			treeStore.Remove (ref iter);
		}
	}
}
