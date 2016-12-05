//
// PackageSpecCreator.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
// Copyright (c) .NET Foundation. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Based on parts of src/NuGet.Core/NuGet.Commands/RestoreCommand/Utility/MSBuildRestoreUtility.cs

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	static class PackageSpecCreator
	{
		public static PackageSpec CreatePackageSpec (DotNetProject project)
		{
			return CreatePackageSpec (new DotNetProjectProxy (project));
		}

		public static PackageSpec CreatePackageSpec (IDotNetProject project)
		{
			var packageSpec = new PackageSpec (GetTargetFrameworks (project));
			packageSpec.FilePath = project.BaseIntermediateOutputPath.Combine (LockFileFormat.AssetsFileName);
			packageSpec.Name = project.Name;
			packageSpec.Version = GetVersion (project);

			packageSpec.RestoreMetadata = CreateRestoreMetadata (packageSpec, project);
			AddPackageReferences (packageSpec, project);

			return packageSpec;
		}

		static IList<TargetFrameworkInformation> GetTargetFrameworks (IDotNetProject project)
		{
			return GetFrameworks (project)
				.Select (framework => new TargetFrameworkInformation {
					FrameworkName = framework
				})
				.ToList();
		}

		static IEnumerable<NuGetFramework> GetFrameworks (IDotNetProject project)
		{
			return GetOriginalTargetFrameworks (project)
				.Select (NuGetFramework.Parse);
		}

		/// <summary>
		/// Return the parsed version or 1.0.0 if the property does not exist.
		/// </summary>
		static NuGetVersion GetVersion (IDotNetProject project)
		{
			string versionString = project.EvaluatedProperties.GetValue ("Version");

			if (string.IsNullOrEmpty(versionString)) {
				// Default to 1.0.0 if the property does not exist
				return new NuGetVersion (1, 0, 0);
			}

			// Snapshot versions are not allowed in .NETCore
			return NuGetVersion.Parse (versionString);
		}

		static ProjectRestoreMetadata CreateRestoreMetadata (PackageSpec packageSpec, IDotNetProject project)
		{
			return new ProjectRestoreMetadata {
				OutputType = RestoreOutputType.NETCore,
				ProjectPath = project.FileName,
				ProjectName = packageSpec.Name,
				ProjectUniqueName = project.FileName,
				OutputPath = project.BaseIntermediateOutputPath,
				OriginalTargetFrameworks = GetOriginalTargetFrameworks (project).ToList ()
			};
		}

		static IEnumerable<string> GetOriginalTargetFrameworks (IDotNetProject project)
		{
			var properties = project.EvaluatedProperties;
			if (properties != null) {
				string targetFramework = properties.GetValue ("TargetFramework");
				if (targetFramework != null) {
					return new [] { targetFramework };
				}

				string targetFrameworks = properties.GetValue ("TargetFrameworks");
				if (targetFrameworks != null) {
					return targetFrameworks.Split (';');
				}
			}

			return new string[0];
		}

		static void AddPackageReferences (PackageSpec packageSpec, IDotNetProject project)
		{
			foreach (var packageReference in project.GetPackageReferences ()) {
				var dependency = new LibraryDependency ();

				dependency.LibraryRange = new LibraryRange (
					name: packageReference.Include,
					versionRange: GetVersionRange (packageReference),
					typeConstraint: LibraryDependencyTarget.Package);

				ApplyIncludeFlags (dependency, packageReference);

				var frameworks = GetProjectFrameworks (project);

				if (frameworks.Count == 0) {
					AddDependencyIfNotExist (packageSpec, dependency);
				} else {
					foreach (var framework in frameworks) {
						AddDependencyIfNotExist (packageSpec, framework, dependency);
					}
				}
			}
		}

		static VersionRange GetVersionRange (ProjectPackageReference packageReference)
		{
			string versionRange = packageReference.Metadata.GetValue ("Version");

			if (!string.IsNullOrEmpty (versionRange)) {
				return VersionRange.Parse (versionRange);
			}

			return VersionRange.All;
		}

		static void ApplyIncludeFlags (LibraryDependency dependency, ProjectPackageReference packageReference)
		{
			var includeFlags = GetIncludeFlags (packageReference.Metadata.GetValue ("IncludeAssets"), LibraryIncludeFlags.All);
			var excludeFlags = GetIncludeFlags (packageReference.Metadata.GetValue ("ExcludeAssets"), LibraryIncludeFlags.None);

			dependency.IncludeType = includeFlags & ~excludeFlags;
			dependency.SuppressParent = GetIncludeFlags (packageReference.Metadata.GetValue ("PrivateAssets"), LibraryIncludeFlagUtils.DefaultSuppressParent);
		}

		static LibraryIncludeFlags GetIncludeFlags (string value, LibraryIncludeFlags defaultValue)
		{
			var parts = Split (value);

			if (parts.Length > 0) {
				return LibraryIncludeFlagUtils.GetFlags (parts);
			} else {
				return defaultValue;
			}
		}

		static string[] Split (string s)
		{
			if (!string.IsNullOrEmpty (s)) {
				return s.Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			}

			return new string[0];
		}

		static HashSet<NuGetFramework> GetProjectFrameworks (IDotNetProject project)
		{
			return new HashSet<NuGetFramework> (
				GetOriginalTargetFrameworks (project).Select (NuGetFramework.Parse));
		}

		static HashSet<NuGetFramework> GetFrameworks (ProjectItem item)
		{
			return new HashSet<NuGetFramework> (
				GetFrameworksStrings (item).Select (NuGetFramework.Parse));
		}

		static HashSet<string> GetFrameworksStrings (ProjectItem item)
		{
			var frameworks = new HashSet<string> (StringComparer.OrdinalIgnoreCase);

			string frameworksString = item.Metadata.GetValue ("TargetFrameworks");

			if (!string.IsNullOrEmpty(frameworksString)) {
				frameworks.UnionWith (frameworksString.Split(';'));
			}

			return frameworks;
		}

		private static bool AddDependencyIfNotExist (PackageSpec packageSpec, LibraryDependency dependency)
		{
			if (!packageSpec.Dependencies
				.Select (d => d.Name)
				.Contains (dependency.Name, StringComparer.OrdinalIgnoreCase)) {

				packageSpec.Dependencies.Add (dependency);

				return true;
			}

			return false;
		}

		static bool AddDependencyIfNotExist (PackageSpec packageSpec, NuGetFramework framework, LibraryDependency dependency)
		{
			var frameworkInfo = packageSpec.GetTargetFramework (framework);

			if (!packageSpec.Dependencies
				.Concat (frameworkInfo.Dependencies)
				.Select (d => d.Name)
				.Contains (dependency.Name, StringComparer.OrdinalIgnoreCase)) {

				frameworkInfo.Dependencies.Add (dependency);

				return true;
			}

			return false;
		}
	}
}
