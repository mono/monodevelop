
using System;
using Gtk;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	public partial class SelectRepositoryDialog : Gtk.Dialog
	{
		TreeStore store;
		
		public SelectRepositoryDialog (string regPath, string startPath)
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(string));
			tree.AppendColumn ("", new CellRendererText (), "text", 2);
			tree.Model = store;
			TreeIter selIter = TreeIter.Zero;
			
			foreach (RegistryExtensionNode reg in AddinAuthoringService.GetRegistries ()) {
				string[] startupFolders = AddinRegistry.GetRegisteredStartupFolders (reg.RegistryPath);
				if (startupFolders.Length == 0) {
					store.AppendValues (reg.RegistryPath, reg.RegistryPath, reg.Name);
				}
				else if (startupFolders.Length == 1) {
					TreeIter it = store.AppendValues (reg.RegistryPath, startupFolders [0], reg.Name);
					if (reg.RegistryPath == regPath)
						selIter = it;
				}
				else {
					TreeIter it = store.AppendValues (reg.RegistryPath, null, reg.Name);
					foreach (string sf in startupFolders) {
						TreeIter fit = store.AppendValues (it, reg.RegistryPath, sf, sf);
						if (reg.RegistryPath == regPath && sf == startPath)
							selIter = fit;
					}
				}
			}
			if (!selIter.Equals (TreeIter.Zero))
				tree.Selection.SelectIter (selIter);
			else {
				if (store.GetIterFirst (out selIter))
					tree.Selection.SelectIter (selIter);
			}
			UpdateButtons ();
			
			tree.Selection.Changed += delegate {
				UpdateButtons ();
			};
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
	}
}
