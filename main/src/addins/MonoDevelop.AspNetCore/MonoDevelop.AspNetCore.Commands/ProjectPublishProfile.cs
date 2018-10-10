using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Xml;
using System.Xml.Serialization;
using System;
using System.IO;

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

		[ItemProperty (Name = nameof (WebPublishMethod))]
		public string WebPublishMethod { get; set; }

		[ItemProperty (Name = nameof (LastUsedBuildConfiguration))]
		public string LastUsedBuildConfiguration { get; set; }

		[ItemProperty (Name = nameof (LastUsedPlatform))]
		public string LastUsedPlatform { get; set; }

		[ItemProperty (Name = nameof (PublishUrl))]
		public string PublishUrl { get; set; }

		[ItemProperty (Name = nameof (DeleteExistingFiles))]
		public string DeleteExistingFiles {
			get {
				return XmlConvert.ToString (deleteExistingFiles);
			}
			set {
				bool parsedValue;

				if (!bool.TryParse (value, out parsedValue))
					deleteExistingFiles = XmlConvert.ToBoolean (value);
			}
		}

		//https://docs.microsoft.com/en-us/dotnet/standard/frameworks
		[ItemProperty (Name = nameof (TargetFramework))]
		public string TargetFramework { get; set; }

		// should be 'true' if a runtime identifier is specified.
		[ItemProperty (Name = nameof (SelfContained))]
		public string SelfContained {
			get => XmlConvert.ToString (selfContained);
			set {
				bool parsedValue;

				if (!bool.TryParse (value, out parsedValue))
					selfContained = XmlConvert.ToBoolean (value);
			}
		}

		[ItemProperty (Name = "_IsPortable")]
		public string IsPortable {
			get => XmlConvert.ToString (isPortable);
			set {
				bool parsedValue;

				if (!bool.TryParse (value, out parsedValue))
					isPortable = XmlConvert.ToBoolean (value);
			}
		} 

		//https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
		[ItemProperty (Name = nameof (RuntimeIdentifier))]
		public string RuntimeIdentifier { get; set; }

	}

	[XmlRoot ("PropertyGroup", Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
	public sealed class ProjectPublishProfile : PublishProfile
	{
		public const string ProjectPublishProfileKey = "FolderProfile";

		public string Name { get; set; }

		public  ProjectPublishProfile () { }

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
					return profile;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to load {0}.", file, ex);
				return null;
			}
		}
	}
}
