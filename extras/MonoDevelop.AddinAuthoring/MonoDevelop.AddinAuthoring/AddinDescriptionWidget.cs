
using System;
using MonoDevelop.Core;
using MonoDevelop.Components;
using Mono.Addins;
using Mono.Addins.Description;
using Gtk;

namespace MonoDevelop.AddinAuthoring
{
	public partial class AddinDescriptionWidget : Gtk.Bin
	{
		AddinData data;
		string defaultId;
		string defaultName;
		
		ListStore storeFiles;
		ListStore storeDeps;
		CellRendererCombo ccombo;
		
		string fileTypeAssembly = GettextCatalog.GetString ("Assembly");
		string fileTypeData = GettextCatalog.GetString ("Data file");

		public event EventHandler Changed;
		
		public AddinDescriptionWidget (AddinData data)
		{
			this.Build();
			
			this.data = data;
			AddinDescription desc = data.AddinManifest;
			comboNs.Entry.Text = desc.Namespace;
			entryVersion.Text = desc.Version;
			entryCompatVersion.Text = desc.CompatVersion;
			textviewDesc.Buffer.Text = desc.Description;
			entryAuthor.Text = desc.Author;
			entryLicense.Text = desc.Copyright;
			entryUrl.Text = desc.Url;
			checkIsRoot.Active = data.AddinManifest.IsRoot;
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			
			NotebookButtonBar butbar = new NotebookButtonBar ();
			butbar.Notebook = notebook;
			mainBox.PackStart (butbar, false, false, 0);
			butbar.Show ();
			
			butbar.SetButton (0, GettextCatalog.GetString ("Header"), "md-addin");
			butbar.SetButton (1, GettextCatalog.GetString ("Dependencies"), "md-reference");
			butbar.SetButton (2, GettextCatalog.GetString ("Extension Points"), "md-extension-point");
			butbar.SetButton (3, GettextCatalog.GetString ("Extensions"), "md-extension-node");

			if (desc.LocalId.Length == 0) {
				defaultId = System.IO.Path.GetFileNameWithoutExtension (data.Project.GetOutputFileName ());
				entryIdentifier.Text = defaultId;
			}
			else
				entryIdentifier.Text = desc.LocalId;
			
			if (desc.Name.Length == 0) {
				defaultName = entryIdentifier.Text;
				entryName.Text = defaultName;
			}
			else
				entryName.Text = desc.Name;
			
			extensionEditor.AddinData = data;
			extensionEditor.BorderWidth = 12;
			extensionEditor.Changed += delegate {
				NotifyChanged ();
			};
			
			extensionPointsEditor.AddinData = data;
			extensionPointsEditor.BorderWidth = 12;
			extensionPointsEditor.Changed += delegate {
				NotifyChanged ();
			};
			
			storeDeps = new ListStore (typeof (object), typeof(string));
			listDeps.Model = storeDeps;
			listDeps.AppendColumn ("", new CellRendererText (), "markup", 1);
			
			storeFiles = new ListStore (typeof(string), typeof(string));
			
			ccombo = new Gtk.CellRendererCombo ();
			ListStore fstore = new ListStore (typeof(string));
			fstore.AppendValues (fileTypeAssembly);
			fstore.AppendValues (fileTypeData);
			ccombo.Model = fstore;
			ccombo.HasEntry = false;
			ccombo.TextColumn = 0;
			ccombo.Editable = true;
			
			// Fill dependencies
			
			foreach (Dependency dep in desc.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null)
					continue;
				AddDependency (adep);
			}
			
			notebook.Page = 0;
		}
		
		public void Save ()
		{
			AddinDescription desc = data.AddinManifest;
			desc.LocalId = entryIdentifier.Text;
			desc.Namespace = comboNs.Entry.Text;
			desc.Version = entryVersion.Text;
			desc.CompatVersion = entryCompatVersion.Text;
			desc.Name = entryName.Text;
			desc.Description = textviewDesc.Buffer.Text;
			desc.Author = entryAuthor.Text;
			desc.Copyright = entryLicense.Text;
			desc.Url = entryUrl.Text;
		}
		
		void AddDependency (AddinDependency adep)
		{
			Addin addin = data.AddinRegistry.GetAddin (adep.FullAddinId, true);
			string name;
			if (addin != null) {
				name = "<b>" + GLib.Markup.EscapeText (addin.Name) + "</b>";
				if (addin.Description.Description.Length > 0)
					name += "\n<small>" + GLib.Markup.EscapeText (addin.Description.Description) + "</small>";
			}
			else {
				name = "<b>" + GLib.Markup.EscapeText (adep.AddinId) + "</b>";
				string txt = GettextCatalog.GetString ("(Add-in not found in repository)");
				name += "\n<small><span foreground='red'>" + GLib.Markup.EscapeText (txt) + "</span></small>";
			}
			storeDeps.AppendValues (adep, name);
		}

		protected virtual void OnEntryChanged(object sender, System.EventArgs e)
		{
			NotifyChanged ();
		}
		
		void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}

		protected virtual void OnButtonAddDepClicked(object sender, System.EventArgs e)
		{
			ExtensionSelectorDialog dlg = new ExtensionSelectorDialog (data.AddinRegistry, null, data.AddinManifest.IsRoot, true);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					foreach (Addin addin in dlg.GetSelection ()) {
						
						bool found = false;
						foreach (Dependency dep in data.AddinManifest.MainModule.Dependencies) {
							if ((dep is AddinDependency) && ((AddinDependency)dep).FullAddinId == addin.Id) {
								found = true;
								break;
							}
						}
						if (found)
							continue;
						
						AddinDependency adep = new AddinDependency ();
						string id = Addin.GetIdName (addin.Id);
						if (data.AddinManifest.Namespace.Length > 0) {
							if (id.StartsWith (data.AddinManifest.Namespace + "."))
								id = id.Substring (data.AddinManifest.Namespace.Length + 1);
							else
								id = "::" + id;
						}
						adep.AddinId = id;
						adep.Version = addin.Version;
						
						data.AddinManifest.MainModule.Dependencies.Add (adep);
						AddDependency (adep);
					}
					extensionEditor.Fill ();
					NotifyChanged ();
				}
			}
			finally {
				dlg.Destroy ();
			}
		}
	
		protected virtual void OnEntryIdentifierChanged(object sender, System.EventArgs e)
		{
			if (entryName.Text == defaultName) {
				entryName.Text = defaultName = entryIdentifier.Text;
			}
		}

		protected virtual void OnButtonRemoveDepClicked(object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (listDeps.Selection.GetSelected (out iter)) {
				Dependency dep = (Dependency) storeDeps.GetValue (iter, 0);
				data.AddinManifest.MainModule.Dependencies.Remove (dep);
				storeDeps.Remove (ref iter);
				if (!iter.Equals (TreeIter.Zero))
					listDeps.Selection.SelectIter (iter);
				extensionEditor.Fill ();
				NotifyChanged ();
			}
		}

		protected virtual void OnButtonAddFileClicked(object sender, System.EventArgs e)
		{
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Select add-in file"));
			try {
				fdiag.SetCurrentFolder (data.Project.BaseDirectory);
				fdiag.SelectMultiple = true;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					foreach (string s in fdiag.Filenames) {
						string rs = FileService.AbsoluteToRelativePath (data.Project.BaseDirectory, s);
						string ext = System.IO.Path.GetExtension (rs).ToLower ();
						if (ext == ".dll" || ext == ".exe") {
							if (!data.AddinManifest.MainModule.Assemblies.Contains (rs)) {
								data.AddinManifest.MainModule.Assemblies.Add (rs);
								storeFiles.AppendValues (rs, fileTypeAssembly);
							}
						}
						else {
							if (!data.AddinManifest.MainModule.DataFiles.Contains (rs)) {
								data.AddinManifest.MainModule.DataFiles.Add (rs);
								storeFiles.AppendValues (rs, fileTypeData);
							}
						}
					}
					if (fdiag.Filenames.Length > 0)
						NotifyChanged ();
				}
			}
			finally {
				fdiag.Destroy ();
			}
		}

		protected void OnCheckIsRootClicked (object sender, System.EventArgs e)
		{
			data.AddinManifest.IsRoot = checkIsRoot.Active;
			NotifyChanged ();
		}
	}
}
