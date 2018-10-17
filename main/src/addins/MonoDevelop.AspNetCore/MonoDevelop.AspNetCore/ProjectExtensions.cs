using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	static class ProjectExtensions
	{
		public static IEnumerable<ProjectPublishProfile> GetPublishProfiles (this DotNetProject project)
		{
			var profileFiles = Directory.EnumerateFiles (project.BaseDirectory.Combine ("Properties", "PublishProfiles"), "*.pubxml");

			foreach (var file in profileFiles) {
				var profile = ProjectPublishProfile.ReadModel (file);
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
			string profileFileContents = null;
			var ns = new XmlSerializerNamespaces ();
			ns.Add ("", "http://schemas.microsoft.com/developer/msbuild/2003");
			var xmlSerializer = new XmlSerializer (profile.GetType ());
			using (var stream = new MemoryStream ()) {
				using (var xmlWriter = new XmlTextWriter (stream, Encoding.UTF8) { Formatting = Formatting.Indented }) {
					xmlWriter.WriteStartDocument ();
					xmlWriter.WriteStartElement ("Project");
					xmlWriter.WriteAttributeString ("ToolsVersion", "4.0");
					xmlWriter.WriteAttributeString ("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");
					xmlSerializer.Serialize (xmlWriter, profile, ns); 
					xmlWriter.WriteEndElement ();
					xmlWriter.WriteEndDocument ();
					xmlWriter.Flush ();
					stream.Position = 0;

					using (var reader = new StreamReader (stream)) {
						profileFileContents = reader.ReadToEnd ();
					}
				}
			}

			var profileFileName = project.BaseDirectory.Combine ("Properties", "PublishProfiles", project.GetNextPubXmlFileName ());

			string publishProfilesDirectory = Path.GetDirectoryName (profileFileName);

			Directory.CreateDirectory (publishProfilesDirectory);

			File.WriteAllText (profileFileName, profileFileContents);

			project.AddFile (profileFileName);

			return true;
		}

		static string GetNextPubXmlFileName (this DotNetProject project)
		{
			var baseDirectory = project.BaseDirectory.Combine ("Properties", "PublishProfiles");
			var identifier = string.Empty;
			var count = default (int);
			var file = $"{ProjectPublishProfile.ProjectPublishProfileKey}{identifier}.pubxml";

			while (File.Exists (Path.Combine (baseDirectory, file))) {
				identifier = $" {++count}";
				file = $"{ProjectPublishProfile.ProjectPublishProfileKey}{identifier}.pubxml";
			}

			return Path.Combine (baseDirectory, file);
		}
	}
}
