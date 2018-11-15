using System.IO;
using System.Linq;
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

			if (!ProjectSupportsFolderPublishing (project)) {
				return;
			}

			if (!project.GetPublishProfilesDirectory ().Exists)
				return;

			var profiles = project.GetPublishProfiles ();
			foreach (var profile in profiles.OrderBy (x => x.Name)) {
				if (profile.WebPublishMethod == "FileSystem")
					info.Add (GettextCatalog.GetString ("Publish to {0} - {1}", profile.Name, profile.WebPublishMethod), new PublishCommandItem (project, profile));
			}
		}
	}
}