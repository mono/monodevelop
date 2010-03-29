
using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using Mono.Addins;
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinDescriptionView: AbstractViewContent
	{
		AddinDescriptionWidget descWidget;
		AddinData data;
		AddinDescription adesc;
		DateTime descTimestamp;
		bool inInternalUpdate;
		
		public AddinDescriptionView (AddinData data)
		{
			this.data = data;
			ContentName = data.Project.Name + " Extension Model";
			//ContentName = data.AddinManifestFileName;
			Project = data.Project;
			
			descWidget = new AddinDescriptionWidget ();
			descWidget.Changed += delegate {
				IsDirty = true;
			};

			data.Changed += OnDataChanged;
			
			Reload ();
		}
		
		public override void Dispose ()
		{
			data.Changed -= OnDataChanged;
			base.Dispose ();
		}

		
		public override string StockIconId {
			get { return "md-addin"; }
		}

		
		public override void Load (string fileName)
		{
		}
		
		public override Gtk.Widget Control {
			get { return descWidget; }
		}
		
		public override bool IsFile {
			get { return false; }
		}

		public AddinData Data {
			get {
				return data;
			}
		}

		public AddinDescription AddinDescription {
			get {
				return adesc;
			}
		}
		
		public override void Save ()
		{
			descWidget.Save ();
			AddinAuthoringService.SaveFormatted (adesc);
			IsDirty = false;
			data.NotifyChanged (true);
		}
		
		internal void BeginInternalUpdate ()
		{
			inInternalUpdate = true;
		}
		
		internal void EndInternalUpdate ()
		{
			inInternalUpdate = false;
			descTimestamp = File.GetLastWriteTime (data.AddinManifestFileName);
		}
		
		public void Update ()
		{
			descWidget.Update ();
		}
		
		public void Reload ()
		{
			adesc = data.AddinRegistry.ReadAddinManifestFile (data.AddinManifestFileName);
			descWidget.Fill (adesc, data);
			IsDirty = false;
			descTimestamp = File.GetLastWriteTime (data.AddinManifestFileName);
		}
		
		void OnDataChanged (object s, EventArgs a)
		{
			if (inInternalUpdate || descTimestamp == File.GetLastWriteTime (data.AddinManifestFileName))
				return;
			if (IsDirty) {
				string q = AddinManager.CurrentLocalizer.GetString ("The add-in manifest for project '{0}' has been modified. Do you want to reload it? (unsaved changes will be lost)", data.Project.Name);
				if (!MessageService.Confirm (q, AlertButton.Reload))
					return;
			}
			Reload ();
		}
		
		public void ShowExtensionPoints ()
		{
			descWidget.ShowExtensionPoints ();
		}
		
		public void ShowExtensions ()
		{
			descWidget.ShowExtensions ();
		}
	}
}
