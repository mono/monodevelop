using System;
using System.Collections.Generic;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	public static class ProjectExtensions
	{
		public static ProjectPublishProfile [] GetPublishProfiles (this DotNetProject project)
		{
			if (project == null || !project.UserProperties.HasValue (ProjectPublishProfile.ProjectPublishProfileKey)) {
				return new ProjectPublishProfile [0];
			}

			var profiles = new List<ProjectPublishProfile> ();

			foreach (var item in project.UserProperties.GetValue<ProjectPublishProfile []> (ProjectPublishProfile.ProjectPublishProfileKey)) {
				profiles.Add (item);
			}

			return profiles.ToArray ();
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
	}
}
