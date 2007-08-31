using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;

namespace MonoDevelop.Autotools
{
	public class MakefileOptionPanel : AbstractOptionPanel
	{
		MakefileOptionPanelWidget widget;

		public MakefileOptionPanel ()
		{
		}

		public override void LoadPanelContents()
		{
			try {
				Project project = ((Properties) CustomizationObject).Get<Project> ("Project");
				MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;

				MakefileData tmpData = null;
				if (data != null) {
					tmpData = (MakefileData) data.Clone ();
				}
				Add (widget = new MakefileOptionPanelWidget (project, tmpData));
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public override bool StorePanelContents()
		{
			Project project = ((Properties) CustomizationObject).Get<Project> ("Project");
			return widget.Store (project);
		}
	}
}
