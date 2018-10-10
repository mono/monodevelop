using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	static class ProjectExtensions
	{
		public static IEnumerable<ProjectPublishProfile> GetPublishProfiles (this DotNetProject project)
		{
			var profileFiles = Directory.GetFiles (project.BaseDirectory.Combine ("Properties", "PublishProfiles"), "*.pubxml");

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
			using (var stream = new MemoryStream ()) {
				using (var xmlWriter = new XmlTextWriter (stream, Encoding.UTF8) { Formatting = Formatting.Indented }) {
					xmlWriter.WriteStartDocument ();
					xmlWriter.WriteStartElement ("Project");
					xmlWriter.WriteAttributeString ("ToolsVersion", "4.0");
					xmlWriter.WriteAttributeString ("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");
					xmlWriter.WriteStartElement ("PropertyGroup");
					var propertyInfo = profile.GetType ().GetProperties ()
							.Where (prop => prop.PropertyType == typeof (string) || prop.PropertyType == typeof (bool))
							.OrderBy (p => p.GetCustomAttributes (typeof (ItemProperty), true)
							.Cast<ItemProperty> ()
							.Select (a => a.Name)
							.FirstOrDefault ());
					foreach (var pi in propertyInfo) {
						if (pi.GetValue (profile, null) != null) {
							xmlWriter.WriteElementString (pi.Name, pi.GetValue (profile, null).ToString ());
						}
					}
					xmlWriter.WriteEndElement ();
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
			if (!Directory.Exists (publishProfilesDirectory)) {
				Directory.CreateDirectory (publishProfilesDirectory);
			}

			File.WriteAllText (profileFileName, profileFileContents);

			project.AddFile (profileFileName);

			return true;
		}

		static string GetNextPubXmlFileName (this DotNetProject project)
		{
			var baseDirectory = project.BaseDirectory.Combine ("Properties", "PublishProfiles");
			var identifier = string.Empty;
			var count = default (int);

			while (File.Exists (Path.Combine (baseDirectory, $"{ProjectPublishProfile.ProjectPublishProfileKey}{identifier}.pubxml"))) {
				identifier = $" {++count}";
			}

			return Path.Combine (baseDirectory, $"{ProjectPublishProfile.ProjectPublishProfileKey}{identifier}.pubxml");
		}
	}
}
