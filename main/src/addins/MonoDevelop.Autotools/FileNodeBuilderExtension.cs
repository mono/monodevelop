
using System;
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
		[CommandHandler (Commands.SynchWithMakefile)]
		public void OnExclude ()
		{
			ProjectFile file = (ProjectFile) CurrentNode.DataItem;
			Project project = file.Project;
			if (project != null) {
				MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
				if (data != null && data.IntegrationEnabled) {
					data.SetFileExcluded (file.FilePath, !data.IsFileExcluded (file.FilePath));
					IdeApp.ProjectOperations.Save (project);
				}
			}
		}
		
		[CommandUpdateHandler (Commands.SynchWithMakefile)]
		public void OnUpdateExclude (CommandInfo cinfo)
		{
			ProjectFile file = (ProjectFile) CurrentNode.DataItem;
			Project project = file.Project;
			if (project != null) {
				MakefileData data = project.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
				if (data != null && data.IsFileIntegrationEnabled (file.BuildAction)) {
					cinfo.Checked = !data.IsFileExcluded (file.FilePath);
					return;
				}
			}
			cinfo.Visible = false;
		}
	}
}
