
using System;
using Gtk;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	public partial class SelectRepositoryDialog : Gtk.Dialog
	{
		TreeStore store;
		TreeViewState state;
		
		public SelectRepositoryDialog (string regPath, string startPath)
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(string), typeof(RegistryExtensionNode));
			tree.AppendColumn ("", new CellRendererText (), "text", 2);
			tree.Model = store;
			state = new TreeViewState (tree, 0);
			
			Fill (regPath, startPath);
			
			tree.Selection.Changed += delegate {
				UpdateButtons ();
			};
		}
		
		void Fill (string regPath, string startPath)
		{
			state.Save ();
			store.Clear ();
			
			TreeIter selIter = TreeIter.Zero;
			
			foreach (RegistryExtensionNode reg in AddinAuthoringService.GetRegistries ()) {
				string[] startupFolders = AddinRegistry.GetRegisteredStartupFolders (reg.RegistryPath);
				if (startupFolders.Length == 0) {
					store.AppendValues (reg.RegistryPath, reg.RegistryPath, reg.Name, reg);
				}
				else if (startupFolders.Length == 1) {
					TreeIter it = store.AppendValues (reg.RegistryPath, startupFolders [0], reg.Name, reg);
					if (reg.RegistryPath == regPath)
						selIter = it;
				}
				else {
					TreeIter it = store.AppendValues (reg.RegistryPath, null, reg.Name, reg);
					foreach (string sf in startupFolders) {
						TreeIter fit = store.AppendValues (it, reg.RegistryPath, sf, sf, reg);
						if (reg.RegistryPath == regPath && sf == startPath)
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
		
		public string RegistryPath {
			get {
				TreeIter it;
				if (tree.Selection.GetSelected (out it))
					return (string) store.GetValue (it, 0);
				else
					return null;
			}
		}
		
		public string StartupPath {
			get {
				TreeIter it;
				if (tree.Selection.GetSelected (out it))
					return (string) store.GetValue (it, 1);
				else
					return null;
			}
		}
		
		void UpdateButtons ()
		{
			buttonOk.Sensitive = RegistryPath != null && StartupPath != null;
		}

		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			NewRegistryDialog dlg = new NewRegistryDialog ();
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				RegistryExtensionNode reg = new RegistryExtensionNode ();
				reg.Name = dlg.RegistryName;
				reg.RegistryPath = dlg.RegistryPath;
				AddinAuthoringService.AddCustomRegistry (reg);
				Fill (null, null);
			}
			dlg.Destroy ();
		}

		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			string q = AddinManager.CurrentLocalizer.GetString ("Are you sure you want to remove this registry reference?");
			if (IdeApp.Services.MessageService.AskQuestion (q)) {
				TreeIter it;
				tree.Selection.GetSelected (out it);
				RegistryExtensionNode reg = (RegistryExtensionNode) store.GetValue (it, 3);
				AddinAuthoringService.RemoveCustomRegistry (reg);
				Fill (null, null);
			}
		}
	}
}
