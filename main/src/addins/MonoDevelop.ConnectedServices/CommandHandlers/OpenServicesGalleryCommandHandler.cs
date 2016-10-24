using System;
using System.Linq;
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
			var binding = project.GetConnectedServicesBinding ();
			info.Visible = info.Enabled = binding.HasSupportedServices;

			var serviceId = info.DataItem as string;
			if (!string.IsNullOrEmpty (serviceId))
				info.Visible = info.Enabled = binding.SupportedServices.Any (s => s.Id == serviceId);
		}

		protected override void Run (object dataItem)
		{
			var serviceId = dataItem as string;
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			var binding = project.GetConnectedServicesBinding ();

			if (!string.IsNullOrEmpty (serviceId) && binding.SupportedServices.Any (s => s.Id == serviceId))
				ConnectedServices.OpenServicesTab (project, serviceId);
			else
				base.Run (dataItem);
		}

		protected override void Run ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			ConnectedServices.OpenServicesTab (project);
		}
	}
}
