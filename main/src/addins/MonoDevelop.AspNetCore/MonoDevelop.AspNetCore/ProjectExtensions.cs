using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	static class ProjectExtensions
	{
		public static IEnumerable<ProjectPublishProfile> GetPublishProfiles (this DotNetProject project)
		{
			var profileFiles = project.GetPublishProfilesDirectory ().EnumerateFiles ("*.pubxml");

			foreach (var file in profileFiles) {
				var profile = ProjectPublishProfile.ReadModel (file.FullName);
				if (profile != null)
					yield return profile;
			}
		}

		public static string GetActiveConfiguration (this DotNetProject project)
		{
			return project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration)?.Name;
		}

		public static string GetActivePlatform (this DotNetProject project)
		{
			var platform = project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration)?.Platform;
			if (string.IsNullOrEmpty (platform))
				platform = "AnyCPU";
			return platform;
		}

		public static bool CreatePublishProfileFile (this DotNetProject project, ProjectPublishProfile profile)
		{
			string profileFileContents = ProjectPublishProfile.WriteModel (profile);

			var profileFileName = project.BaseDirectory.Combine ("Properties", "PublishProfiles", project.GetNextPubXmlFileName ());

			string publishProfilesDirectory = Path.GetDirectoryName (profileFileName);

			Directory.CreateDirectory (publishProfilesDirectory);

			File.WriteAllText (profileFileName, profileFileContents);

			project.AddFile (profileFileName);

			return true;
		}

		static string GetNextPubXmlFileName (this DotNetProject project)
		{
			var baseDirectory = project.GetPublishProfilesDirectory ().FullName;
			var identifier = string.Empty;
			var count = default (int);
			var file = $"{ProjectPublishProfile.ProjectPublishProfileKey}{identifier}.pubxml";

			while (File.Exists (Path.Combine (baseDirectory, file))) {
				identifier = $" {++count}";
				file = $"{ProjectPublishProfile.ProjectPublishProfileKey}{identifier}.pubxml";
			}

			return Path.Combine (baseDirectory, file);
		}

		public static DirectoryInfo GetPublishProfilesDirectory (this DotNetProject project)
			=> new DirectoryInfo (project.BaseDirectory.Combine ("Properties", "PublishProfiles"));
	}
}
