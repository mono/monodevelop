
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinDescriptionView: AbstractViewContent
	{
		AddinDescriptionWidget descWidget;
		AddinData data;
		
		public AddinDescriptionView (AddinData data)
		{
			this.data = data;
			ContentName = data.Project.Name + " Add-in Description";
			descWidget = new AddinDescriptionWidget (data);
			descWidget.Changed += delegate {
				IsDirty = true;
			};
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
		
		public override void Save ()
		{
			descWidget.Save ();
			data.AddinManifest.Save ();
			IsDirty = false;
		}
	}
}
