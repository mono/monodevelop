using System;
using System.Collections.Generic;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	static class ProjectExtensions
	{
		public static ProjectPublishProfile [] GetPublishProfiles (this DotNetProject project)
		{
			return project == null || !project.UserProperties.HasValue (ProjectPublishProfile.ProjectPublishProfileKey)
				? Array.Empty<ProjectPublishProfile> ()
				: project.UserProperties.GetValue<ProjectPublishProfile []> (ProjectPublishProfile.ProjectPublishProfileKey);
		}

		public static void AddPublishProfiles (this DotNetProject project, ProjectPublishProfile newEntry)
		{
			var profiles = new List<ProjectPublishProfile> ();
			if (project.UserProperties.HasValue (ProjectPublishProfile.ProjectPublishProfileKey)) {
				foreach (var item in project.UserProperties.GetValue<ProjectPublishProfile []> (ProjectPublishProfile.ProjectPublishProfileKey)) {
					if (item.Name.IndexOf (newEntry.Name, StringComparison.OrdinalIgnoreCase) < 0)
						profiles.Add (item);
				}
			}
			profiles.Add (newEntry);
			project.UserProperties.SetValue<ProjectPublishProfile []> (ProjectPublishProfile.ProjectPublishProfileKey, profiles.ToArray());
		}

		public static string GetActiveConfiguration (this DotNetProject project)
		{
			return project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration)?.Name;
		}
	}
}
