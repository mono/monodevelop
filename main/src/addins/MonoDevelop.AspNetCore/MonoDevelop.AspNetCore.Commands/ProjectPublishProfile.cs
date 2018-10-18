using System.Xml;
using System.Xml.Serialization;
using System;
using System.IO;
using MonoDevelop.Core;
using System.Text;

namespace MonoDevelop.AspNetCore.Commands
{
	/*
		<PropertyGroup>
		    <PublishProtocol>FileSystem</PublishProtocol>
		    <Configuration>Debug</Configuration>
		    <Platform>Any CPU</Platform>
		    <TargetFramework>netcoreapp2.0</TargetFramework>
		    <PublishDir>bin\Debug\netcoreapp2.0\publish4\</PublishDir>
		    <SelfContained>false</SelfContained>
		    <_IsPortable>true</_IsPortable>
		</PropertyGroup>
	*/

	public abstract class PublishProfile
	{
		bool deleteExistingFiles;
		bool selfContained;
		bool isPortable = true;

		protected PublishProfile ()
		{
			WebPublishMethod = "FileSystem";
		}

		[XmlElement]
		public string WebPublishMethod { get; set; }

		[XmlElement]
		public string LastUsedBuildConfiguration { get; set; }

		[XmlElement]
		public string LastUsedPlatform { get; set; }

		[XmlElement (ElementName = "publishUrl")]
		public string PublishUrl { get; set; }

		[XmlElement]
		public string DeleteExistingFiles {
			get {
				return XmlConvert.ToString (deleteExistingFiles);
			}
			set {
				if (!bool.TryParse (value, out var parsedValue))
					deleteExistingFiles = XmlConvert.ToBoolean (value);
			}
		}

		//https://docs.microsoft.com/en-us/dotnet/standard/frameworks
		[XmlElement]
		public string TargetFramework { get; set; }

		// should be 'true' if a runtime identifier is specified.
		[XmlElement]
		public string SelfContained {
			get => XmlConvert.ToString (selfContained);
			set {
				if (!bool.TryParse (value, out var parsedValue))
					selfContained = XmlConvert.ToBoolean (value);
			}
		}

		[XmlElement (ElementName = "_IsPortable")]
		public string IsPortable {
			get => XmlConvert.ToString (isPortable);
			set {
				if (!bool.TryParse (value, out var parsedValue))
					isPortable = XmlConvert.ToBoolean (value);
			}
		}

		//https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
		[XmlElement (ElementName = nameof (RuntimeIdentifier))]
		public string RuntimeIdentifier { get; set; }

	}

	[XmlRoot ("PropertyGroup", Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
	public class ProjectPublishProfile : PublishProfile
	{
		public const string ProjectPublishProfileKey = "FolderProfile";

		public string Name { get; set; }

		public ProjectPublishProfile () { }

		public static ProjectPublishProfile ReadModel (string file)
		{
			try {
				var serializer = new XmlSerializer (typeof (ProjectPublishProfile), "http://schemas.microsoft.com/developer/msbuild/2003");

				using (var xmlReader = new XmlTextReader (file)) {
					var xmldoc = new XmlDocument ();
					xmldoc.Load (xmlReader);

					var root = xmldoc.DocumentElement;
					var profileNode = root.FirstChild;
					var profile = (ProjectPublishProfile)serializer.Deserialize (new XmlNodeReader (root.FirstChild));
					profile.Name = Path.GetFileNameWithoutExtension (file);

					if (Path.DirectorySeparatorChar != '\\')
						profile.PublishUrl = profile.PublishUrl?.Replace ('\\', Path.DirectorySeparatorChar);

					return profile;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to load {0}.", file, ex);
				return null;
			}
		}

		public static string WriteModel (ProjectPublishProfile profile)
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

			return profileFileContents;
		}
	}
}
