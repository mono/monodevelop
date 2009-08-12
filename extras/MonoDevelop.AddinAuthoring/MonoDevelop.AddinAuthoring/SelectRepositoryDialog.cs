
using System;
using Gtk;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	public partial class SelectRepositoryDialog : Gtk.Dialog
	{
		TreeStore store;
		TreeViewState state;
		
		public SelectRepositoryDialog (RegistryInfo selection)
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(string), typeof(RegistryInfo));
			tree.AppendColumn ("", new CellRendererText (), "text", 3);
			tree.Model = store;
			state = new TreeViewState (tree, 0);
			
			Fill (selection);
			
			tree.Selection.Changed += delegate {
				UpdateButtons ();
			};
		}
		
		void Fill (RegistryInfo selection)
		{
			state.Save ();
			store.Clear ();
			
			TreeIter selIter = TreeIter.Zero;
			
			foreach (RegistryInfo reg in AddinAuthoringService.GetRegistries ()) {
				string[] startupFolders = AddinRegistry.GetRegisteredStartupFolders (reg.RegistryPath);
				if (startupFolders.Length == 0) {
					store.AppendValues (reg.RegistryPath, reg.RegistryPath, reg.ApplicationName, reg.ApplicationName, reg);
				}
				else if (startupFolders.Length == 1) {
					TreeIter it = store.AppendValues (reg.RegistryPath, startupFolders [0], reg.ApplicationName, reg.ApplicationName, reg);
					if (reg.RegistryPath == selection.RegistryPath)
						selIter = it;
				}
				else {
					TreeIter it = store.AppendValues (reg.RegistryPath, null, reg.ApplicationName, reg.ApplicationName, reg);
					foreach (string sf in startupFolders) {
						TreeIter fit = store.AppendValues (it, reg.RegistryPath, sf, sf, reg.ApplicationName, reg);
						if (reg.RegistryPath == selection.RegistryPath && sf == selection.ApplicationPath)
							selIter = fit;
					}
				}
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
		
		public RegistryInfo RegistryInfo {
			get {
				TreeIter it;
				if (tree.Selection.GetSelected (out it)) {
					RegistryInfo info = new RegistryInfo ();
					info.ApplicationName = (string) store.GetValue (it, 2);
					info.RegistryPath = (string) store.GetValue (it, 0);
					info.ApplicationPath = (string) store.GetValue (it, 1);
					if (info.ApplicationPath == null)
						return null;
					else
						return info;
				}
				else
					return null;
			}
		}
		
		void UpdateButtons ()
		{
			buttonOk.Sensitive = RegistryInfo != null;
		}

		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			NewRegistryDialog dlg = new NewRegistryDialog (null);
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				RegistryInfo reg = new RegistryInfo ();
				reg.ApplicationName = dlg.ApplicationName;
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
				RegistryInfo reg = (RegistryInfo) store.GetValue (it, 3);
				AddinAuthoringService.RemoveCustomRegistry (reg);
				Fill (null);
			}
		}
	}
}
