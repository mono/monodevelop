
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
		AddinDescription adesc;
		string defaultId;
		string defaultName;
		NotebookButtonBar butbar;
		
		public event EventHandler Changed;
		
		public AddinDescriptionWidget ()
		{
			this.Build();
			
			butbar = new NotebookButtonBar ();
			butbar.Notebook = notebook;
			mainBox.PackStart (butbar, false, false, 0);
			butbar.Show ();
			
			butbar.SetButton (0, AddinManager.CurrentLocalizer.GetString ("Header"), "gtk-dialog-info");
			butbar.SetButton (1, AddinManager.CurrentLocalizer.GetString ("Extension Points"), "md-extension-point");
			butbar.SetButton (2, AddinManager.CurrentLocalizer.GetString ("Extensions"), "md-extension-node");

			extensionEditor.BorderWidth = 12;
			extensionEditor.Changed += delegate {
				NotifyChanged ();
			};
			
			extensionPointsEditor.BorderWidth = 12;
			extensionPointsEditor.Changed += delegate {
				NotifyChanged ();
			};
			
			notebook.Page = 0;
		}
		
		public void Fill (AddinDescription desc, AddinData data)
		{
			adesc = desc;
			comboNs.Entry.Text = desc.Namespace;
			entryVersion.Text = desc.Version;
			entryCompatVersion.Text = desc.CompatVersion;
			textviewDesc.Buffer.Text = desc.Description;
			entryAuthor.Text = desc.Author;
			entryLicense.Text = desc.Copyright;
			entryUrl.Text = desc.Url;
			checkIsRoot.Active = desc.IsRoot;
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			
			if (desc.LocalId.Length == 0) {
				defaultId = System.IO.Path.GetFileNameWithoutExtension (data.Project.GetOutputFileName (data.Project.DefaultConfigurationId));
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
			
			extensionEditor.SetData (adesc, data);
			extensionPointsEditor.SetData (adesc, data);
		}
		
		public void Save ()
		{
			adesc.LocalId = entryIdentifier.Text;
			adesc.Namespace = comboNs.Entry.Text;
			adesc.Version = entryVersion.Text;
			adesc.CompatVersion = entryCompatVersion.Text;
			adesc.Name = entryName.Text;
			adesc.Description = textviewDesc.Buffer.Text;
			adesc.Author = entryAuthor.Text;
			adesc.Copyright = entryLicense.Text;
			adesc.Url = entryUrl.Text;
		}
		
		public void Update ()
		{
			extensionEditor.Fill ();
		}
		
		public void ShowExtensionPoints ()
		{
			butbar.ShowPage (1);
		}
		
		public void ShowExtensions ()
		{
			butbar.ShowPage (2);
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


		protected virtual void OnEntryIdentifierChanged(object sender, System.EventArgs e)
		{
			if (entryName.Text == defaultName) {
				entryName.Text = defaultName = entryIdentifier.Text;
			}
		}

		protected void OnCheckIsRootClicked (object sender, System.EventArgs e)
		{
			adesc.IsRoot = checkIsRoot.Active;
			NotifyChanged ();
		}
	}
}
