
using System;
using MonoDevelop.Core.Collections;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Autotools
{
	class FileNodeBuilderExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFile).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(FileNodeCommandHandler); }
		}
	}
	
	class FileNodeCommandHandler: NodeCommandHandler
	{
		const string infoProperty = "MonoDevelop.Autotools.MakefileInfo";
		
		[CommandHandler (Commands.SynchWithMakefile)]
		[AllowMultiSelection]
		public void OnExclude ()
		{
			//if all of the selection is already checked, then toggle checks them off
			//else it turns them on. hence we need to find if they're all checked,
			bool allChecked = true;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				if (file.Project != null) {
					MakefileData data = file.Project.ExtendedProperties [infoProperty] as MakefileData;
					if (data != null && data.IsFileIntegrationEnabled (file.BuildAction)) {
						if (data.IsFileExcluded (file.FilePath)) {
							allChecked = false;
							break;
						}
					}
				}
			}
			
			Set<SolutionEntityItem> projects = new Set<SolutionEntityItem> ();
			
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				if (file.Project != null) {
					projects.Add (file.Project);
					MakefileData data = file.Project.ExtendedProperties [infoProperty] as MakefileData;
					if (data != null && data.IntegrationEnabled) {
						data.SetFileExcluded (file.FilePath, allChecked);
					}
				}
			}
				
			IdeApp.ProjectOperations.Save (projects);
		}
		
		[CommandUpdateHandler (Commands.SynchWithMakefile)]
		public void OnUpdateExclude (CommandInfo cinfo)
		{
			bool anyChecked = false;
			bool allChecked = true;
			bool anyEnabled = false;
			bool allEnabled = true;
			
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				if (file.Project != null) {
					MakefileData data = file.Project.ExtendedProperties [infoProperty] as MakefileData;
					if (data != null && data.IsFileIntegrationEnabled (file.BuildAction)) {
						anyEnabled = true;
						if (!data.IsFileExcluded (file.FilePath)) {
							anyChecked = true;
						} else {
							allChecked = false;
						}
					} else {
						allEnabled = false;
					}
				}
			}
				
			cinfo.Visible = anyEnabled;
			cinfo.Enabled = anyEnabled && allEnabled;
			cinfo.Checked = anyChecked;
			cinfo.CheckedInconsistent = anyChecked && !allChecked;
		}
	}
}
