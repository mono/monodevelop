
using System;
using Gtk;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;

namespace MonoDevelop.AddinAuthoring
{
	public partial class SelectRepositoryDialog : Gtk.Dialog
	{
		ListStore store;
		TreeViewState state;
		
		public SelectRepositoryDialog (string selection)
		{
			this.Build();
			
			store = new ListStore (typeof(string), typeof(string), typeof(RegistryInfo));
			tree.AppendColumn (AddinManager.CurrentLocalizer.GetString ("Application"), new CellRendererText (), "text", 0);
			tree.AppendColumn (AddinManager.CurrentLocalizer.GetString ("Description"), new CellRendererText (), "text", 1);
			tree.Model = store;
			state = new TreeViewState (tree, 0);
			
			Fill (selection);
			
			tree.Selection.Changed += delegate {
				UpdateButtons ();
			};
		}
		
		void Fill (string selection)
		{
			state.Save ();
			store.Clear ();
			
			TreeIter selIter = TreeIter.Zero;
			
			foreach (RegistryInfo reg in AddinAuthoringService.GetRegistries ()) {
				TreeIter it = store.AppendValues (reg.ApplicationName, reg.Description, reg);
				if (reg.ApplicationName == selection)
					selIter = it;
			}
			state.Load ();
			
			if (!selIter.Equals (TreeIter.Zero))
				tree.Selection.SelectIter (selIter);
			else {
				if (!tree.Selection.GetSelected (out selIter) && store.GetIterFirst (out selIter))
					tree.Selection.SelectIter (selIter);
			}
			UpdateButtons ();
		}
		
		public RegistryInfo SelectedApplication {
			get {
				TreeIter it;
				if (tree.Selection.GetSelected (out it))
					return (RegistryInfo) store.GetValue (it, 2);
				else
					return null;
			}
		}
		
		void UpdateButtons ()
		{
			buttonOk.Sensitive = SelectedApplication != null;
		}

		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			NewRegistryDialog dlg = new NewRegistryDialog (null);
			dlg.TransientFor = this;
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				RegistryInfo reg = new RegistryInfo ();
				reg.ApplicationName = dlg.ApplicationName;
				reg.ApplicationPath = dlg.ApplicationPath;
				reg.RegistryPath = dlg.RegistryPath;
				AddinAuthoringService.AddCustomRegistry (reg);
				Fill (null);
			}
			dlg.Destroy ();
		}

		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			string q = AddinManager.CurrentLocalizer.GetString ("Are you sure you want to remove this registry reference?");
			if (MessageService.Confirm (q, AlertButton.Remove)) {
				TreeIter it;
				tree.Selection.GetSelected (out it);
				RegistryInfo reg = (RegistryInfo) store.GetValue (it, 2);
				AddinAuthoringService.RemoveCustomRegistry (reg);
				Fill (null);
			}
		}
	}
}
