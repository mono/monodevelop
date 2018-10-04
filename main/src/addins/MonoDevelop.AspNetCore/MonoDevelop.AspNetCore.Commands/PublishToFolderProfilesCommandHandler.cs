using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Commands
{
	sealed class PublishToFolderProfilesCommandHandler : PublishToFolderBaseCommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			base.Update (info);

			project = IdeApp.ProjectOperations.CurrentSelectedProject as DotNetProject;

			if (!ProjectSupportsAzurePublishing (project)) {
				return;
			}


			var profiles = project.GetPublishProfiles ();
			if (profiles.Length > 0) {
				info.AddSeparator ();
			}

			foreach (var profile in profiles) {
				info.Add (GettextCatalog.GetString ("Publish to {0} - Folder", profile.Name), new PublishCommandItem (project, profile));
			}
		}
	}
}