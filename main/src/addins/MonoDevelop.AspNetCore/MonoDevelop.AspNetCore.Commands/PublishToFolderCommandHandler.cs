using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Commands
{
	sealed class PublishToFolderCommandHandler : PublishToFolderBaseCommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;

			if (!ProjectSupportsFolderPublishing (project)) {
				info.Visible = false;			
				return;
			}

			base.Update (info);
		}
	}
}
