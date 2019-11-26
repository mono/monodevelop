using System;
using System.Linq;
using System.Threading;
using MonoDevelop.AspNetCore.Scaffolding;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Commands
{
	enum AspNetCoreCommands
	{
		Scaffold
	}

	class ScaffoldNodeExtension : NodeBuilderExtension
	{
		public override Type CommandHandlerType {
			get { return typeof (ScaffoldCommandHandler); }
		}

		public override bool CanBuildNode (Type dataType)
		{
			return true;
		}
	}

	class ScaffoldCommandHandler : NodeCommandHandler
	{
		[CommandHandler (AspNetCoreCommands.Scaffold)]
		public void Scaffold ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			if (project == null)
				return;

			var folder = CurrentNode.GetParentDataItem (typeof (ProjectFolder), true) as ProjectFolder;
			string parentFolder = folder?.Path ?? project.BaseDirectory;

			Xwt.Toolkit.NativeEngine.Invoke (() => {
				var args = new ScaffolderArgs ();
				var selectionPage = new ScaffolderTemplateSelectPage (args);

				var w = new ScaffolderWizard (project, parentFolder, selectionPage, args);
				var res = w.RunWizard ();
			});
		}

		[CommandUpdateHandler (AspNetCoreCommands.Scaffold)]
		public void ScaffoldUpdate (CommandInfo info)
		{
			var project = CurrentNode.GetParentDataItem (typeof (DotNetProject), true) as DotNetProject;
			info.Enabled = info.Visible = NodeIsProjectOrFolder(CurrentNode) && IsAspNetCoreProject (project);
		}

		static bool NodeIsProjectOrFolder (ITreeNavigator node)
		{
			return node.DataItem is Project || node.DataItem is ProjectFolder;
		}

		bool IsAspNetCoreProject (Project project)
		{
			//TODO: this only checks for SDK style project
			return project != null
				&& (project.MSBuildProject.GetReferencedSDKs ().FirstOrDefault (x => x.IndexOf ("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) != -1) != null);
		}
	}
}
