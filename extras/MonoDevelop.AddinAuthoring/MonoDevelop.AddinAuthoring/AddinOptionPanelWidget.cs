
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
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
		
		public bool Store ()
		{
			if (checkEnable.Active) {
				string msg = optionsWidget.Validate ();
				if (msg != null) {
					MessageService.ShowError ((Gtk.Window) this.Toplevel, msg);
					return false;
				}
				AddinFeature f = new AddinFeature ();
				f.ApplyFeature (project.ParentCombine, project, optionsWidget);
				return true;
			}
			else {
				AddinData.DisableAddinAuthoringSupport (project);
				return true;
			}
		}

		protected virtual void OnCheckEnableClicked(object sender, System.EventArgs e)
		{
			optionsWidget.Sensitive = checkEnable.Active;
		}
	}
	
	class AddinOptionPanel: AbstractOptionPanel
	{
		AddinOptionPanelWidget widget;
		
		public override void LoadPanelContents ()
		{
			Properties props = (Properties) CustomizationObject;
			CombineEntry entry = props.Get <CombineEntry> ("CombineEntry");
			Add (widget = new AddinOptionPanelWidget ((DotNetProject)entry));
		}
		
		public override bool StorePanelContents ()
		{
			return widget.Store ();
		}
	}
}
