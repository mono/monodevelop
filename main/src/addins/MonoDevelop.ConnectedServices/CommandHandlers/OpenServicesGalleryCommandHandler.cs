using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.ConnectedServices.CommandHandlers
{
	/// <summary>
	/// Command handler to open the services tab
	/// </summary>
	sealed class OpenServicesGalleryCommandHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			base.Update (info);

			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			info.Visible = info.Enabled = project.GetConnectedServicesBinding ().HasSupportedServices;
		}

		protected override void Run ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			ConnectedServices.OpenServicesTab (project);
		}
	}
}
