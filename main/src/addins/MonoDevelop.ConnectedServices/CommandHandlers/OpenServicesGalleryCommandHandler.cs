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
			var hints = GetItemHints (info.DataItem);
			var project = hints.Item1 ?? IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
			var binding = project.GetConnectedServicesBinding ();
			info.Visible = info.Enabled = binding.HasSupportedServices;

			var serviceId = hints.Item2;
			if (!string.IsNullOrEmpty (serviceId))
				info.Visible = info.Enabled = binding.SupportedServices.Any (s => s.Id == serviceId);
		}

		protected override void Run (object dataItem)
		{
			var hints = GetItemHints (dataItem);
			var serviceId = hints.Item2;
			var project = hints.Item1 ?? IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;
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

		static Tuple<DotNetProject, string> GetItemHints (object dataItem)
		{
			var args = dataItem as object [];
			if (args?.Length == 2)
				return Tuple.Create (args [0] as DotNetProject, args [1] as string);

			var project = dataItem as DotNetProject;
			if (project != null)
				return new Tuple<DotNetProject, string> (project, null);

			var serviceId = dataItem as string;
			if (project != null)
				return new Tuple<DotNetProject, string> (null, serviceId);

			return new Tuple<DotNetProject, string> (null, null);
		}
	}
}
