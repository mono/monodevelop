
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	public class MakefileOptionPanel : ItemOptionsPanel
	{
		MakefileOptionPanelWidget widget;

		public MakefileOptionPanel ()
		{
		}

		public override Widget CreatePanelWidget()
		{
			Project project = ConfiguredProject;
			MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;

			MakefileData tmpData = null;
			if (data != null) {
				tmpData = (MakefileData) data.Clone ();
			}
			return (widget = new MakefileOptionPanelWidget (ParentDialog, project, tmpData));
		}
		
		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges (ConfiguredProject);
		}
		
		public override void ApplyChanges ()
		{
			widget.Store (ConfiguredProject);
		}
	}
}
