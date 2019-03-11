//
// NuGetPackageMetadata.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Packaging
{
	class NuGetPackageMetadata
	{
		public const int MaxPackageIdLength = 100;
		public const string PackageIdPropertyName = "PackageId";

		public string Id { get; set; }
		public string Version { get; set; }
		public string Authors { get; set; }
		public string Description { get; set; }

		public string Copyright { get; set; }
		public bool DevelopmentDependency { get; set; }
		public string IconUrl { get; set; }
		public string Language { get; set; }
		public string LicenseUrl { get; set; }
		public string Owners { get; set; }
		public string ProjectUrl { get; set; }
		public string ReleaseNotes { get; set; }
		public bool RequireLicenseAcceptance { get; set; }
		public string Summary { get; set; }
		public string Tags { get; set; }
		public string Title { get; set; }

		public void Load (DotNetProject project)
		{
			Description = project.Description;
			Load (project.MSBuildProject);
		}

		public void UpdateProject (DotNetProject project)
		{
			project.Description = Description;
			Update (project.MSBuildProject);
		}

		void Load (MSBuildProject project)
		{
			MSBuildPropertyGroup propertyGroup = project.GetNuGetMetadataPropertyGroup ();
			Id = GetProperty (propertyGroup, PackageIdPropertyName);
			Version = GetProperty (propertyGroup, "PackageVersion");
			Authors = GetProperty (propertyGroup, "Authors");
			Copyright = GetProperty (propertyGroup, "Copyright");
			DevelopmentDependency = GetProperty (propertyGroup, "DevelopmentDependency", false);
			IconUrl = GetProperty (propertyGroup, "PackageIconUrl");
			Language = GetProperty (propertyGroup, "NeutralLanguage");
			LicenseUrl = GetProperty (propertyGroup, "PackageLicenseUrl");
			Owners = GetProperty (propertyGroup, "Owners");
			ProjectUrl = GetProperty (propertyGroup, "PackageProjectUrl");
			ReleaseNotes = GetProperty (propertyGroup, "PackageReleaseNotes");
			RequireLicenseAcceptance = GetProperty (propertyGroup, "PackageRequireLicenseAcceptance", false);
			Summary = GetProperty (propertyGroup, "Summary");
			Tags = GetProperty (propertyGroup, "PackageTags");
			Title = GetProperty (propertyGroup, "Title");
		}

		string GetProperty (MSBuildPropertyGroup propertyGroup, string name)
		{
			return propertyGroup.GetProperty (name)?.Value;
		}

		bool GetProperty (MSBuildPropertyGroup propertyGroup, string name, bool defaultValue)
		{
			string value = GetProperty (propertyGroup, name);
			if (string.IsNullOrEmpty (value))
				return defaultValue;

			bool result = false;
			if (bool.TryParse (value, out result))
				return result;

			return defaultValue;
		}

		void Update (MSBuildProject project)
		{
			MSBuildPropertyGroup propertyGroup = project.GetNuGetMetadataPropertyGroup ();
			SetProperty (propertyGroup, PackageIdPropertyName, Id);
			SetProperty (propertyGroup, "PackageVersion", Version);
			SetProperty (propertyGroup, "Authors", Authors);
			SetProperty (propertyGroup, "Copyright", Copyright);
			SetProperty (propertyGroup, "DevelopmentDependency", DevelopmentDependency);
			SetProperty (propertyGroup, "PackageIconUrl", IconUrl);
			SetProperty (propertyGroup, "NeutralLanguage", Language);
			SetProperty (propertyGroup, "PackageLicenseUrl", LicenseUrl);
			SetProperty (propertyGroup, "PackageRequireLicenseAcceptance", RequireLicenseAcceptance);
			SetProperty (propertyGroup, "Owners", Owners);
			SetProperty (propertyGroup, "PackageProjectUrl", ProjectUrl);
			SetProperty (propertyGroup, "PackageReleaseNotes", ReleaseNotes);
			SetProperty (propertyGroup, "Summary", Summary);
			SetProperty (propertyGroup, "PackageTags", Tags);
			SetProperty (propertyGroup, "Title", Title);
		}

		void SetProperty (MSBuildPropertyGroup propertyGroup, string name, string value)
		{
			if (string.IsNullOrEmpty (value))
				propertyGroup.RemoveProperty (name);
			else
				propertyGroup.SetValue (name, value);
		}

		void SetProperty (MSBuildPropertyGroup propertyGroup, string name, bool value)
		{
			if (value)
				propertyGroup.SetValue (name, value);
			else
				propertyGroup.RemoveProperty (name);
		}

		public bool IsEmpty ()
		{
			return string.IsNullOrEmpty (Id) &&
				string.IsNullOrEmpty (Version) &&
				string.IsNullOrEmpty (Authors) &&
				string.IsNullOrEmpty (Copyright) &&
				string.IsNullOrEmpty (Description) &&
				!DevelopmentDependency &&
				string.IsNullOrEmpty (IconUrl) &&
				string.IsNullOrEmpty (Language) &&
				string.IsNullOrEmpty (Owners) &&
				string.IsNullOrEmpty (ProjectUrl) &&
				string.IsNullOrEmpty (ReleaseNotes) &&
				!RequireLicenseAcceptance &&
				string.IsNullOrEmpty (Summary) &&
				string.IsNullOrEmpty (Tags) &&
				string.IsNullOrEmpty (Title);
		}
	}
}

