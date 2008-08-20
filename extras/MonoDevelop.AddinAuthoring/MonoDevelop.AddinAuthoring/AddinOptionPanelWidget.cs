
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class AddinOptionPanelWidget : Gtk.Bin
	{
		DotNetProject project;
		
		public AddinOptionPanelWidget (DotNetProject project)
		{
			this.Build();
			this.project = project;
			optionsWidget.Load (project, false);
			
			AddinData data = AddinData.GetAddinData (project);
			if (data != null)
				checkEnable.Active = true;
			else {
				checkEnable.Active = false;
				optionsWidget.Sensitive = false;
			}
				
		}
		
		public bool ValidateChanges ()
		{
			if (checkEnable.Active) {
				string msg = optionsWidget.Validate ();
				if (msg != null) {
					MessageService.ShowError ((Gtk.Window) this.Toplevel, msg);
					return false;
				}
			}
			return true;
		}
		
		public void Store ()
		{
			if (checkEnable.Active) {
				AddinFeature f = new AddinFeature ();
				f.ApplyFeature (project.ParentFolder, project, optionsWidget);
			}
			else {
				AddinData.DisableAddinAuthoringSupport (project);
			}
		}

		protected virtual void OnCheckEnableClicked(object sender, System.EventArgs e)
		{
			optionsWidget.Sensitive = checkEnable.Active;
		}
	}
	
	class AddinOptionPanel: ItemOptionsPanel
	{
		AddinOptionPanelWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return widget = new AddinOptionPanelWidget ((DotNetProject)ConfiguredProject);
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
