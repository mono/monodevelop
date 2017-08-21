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
// and src/NuGet.Clients/NuGet.PackageManagement.VisualStudio/Projects/LegacyPackageReferenceProject.cs

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	static class PackageSpecCreator
	{
		public static PackageSpec CreatePackageSpec (DotNetProject project, DependencyGraphCacheContext context)
		{
			return CreatePackageSpec (new DotNetProjectProxy (project), context);
		}

		public static PackageSpec CreatePackageSpec (IDotNetProject project, DependencyGraphCacheContext context)
		{
			var packageSpec = new PackageSpec (GetTargetFrameworks (project));
			packageSpec.FilePath = project.FileName;
			packageSpec.Name = project.Name;
			packageSpec.Version = GetVersion (project);

			packageSpec.RestoreMetadata = CreateRestoreMetadata (packageSpec, project, context.Settings);
			packageSpec.RuntimeGraph = GetRuntimeGraph (project);
			AddProjectReferences (packageSpec, project, context.Logger);
			AddPackageReferences (packageSpec, project);
			AddPackageTargetFallbacks (packageSpec, project);

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

		static ProjectRestoreMetadata CreateRestoreMetadata (PackageSpec packageSpec, IDotNetProject project, ISettings settings)
		{
			return new ProjectRestoreMetadata {
				ConfigFilePaths = SettingsUtility.GetConfigFilePaths (settings).ToList (),
				FallbackFolders = GetFallbackPackageFolders (settings, project),
				PackagesPath = GetPackagesPath (settings, project),
				ProjectStyle = ProjectStyle.PackageReference,
				ProjectPath = project.FileName,
				ProjectName = packageSpec.Name,
				ProjectUniqueName = project.FileName,
				ProjectWideWarningProperties = GetWarningProperties (project),
				OutputPath = project.BaseIntermediateOutputPath,
				OriginalTargetFrameworks = GetOriginalTargetFrameworks (project).ToList (),
				Sources = GetSources (settings, project)
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
					return MSBuildStringUtility.Split (targetFrameworks);
				}
			}

			var framework = NuGetFramework.Parse (project.TargetFrameworkMoniker.ToString ());
			return new [] { framework.GetShortFolderName () };
		}

		static void AddProjectReferences (PackageSpec spec, IDotNetProject project, ILogger logger)
		{
			// Add groups for each spec framework
			var frameworkGroups = new Dictionary<NuGetFramework, List<ProjectRestoreReference>> ();
			foreach (var framework in spec.TargetFrameworks.Select (e => e.FrameworkName).Distinct ()) {
				frameworkGroups.Add (framework, new List<ProjectRestoreReference> ());
			}

			var flatReferences = project.References.Where (IsProjectReference)
				.Select (projectReference => GetProjectRestoreReference (projectReference, project, logger))
				.Where (projectReference => projectReference != null);

			// Add project paths
			foreach (var frameworkPair in flatReferences) {
				// If no frameworks were given, apply to all
				var addToFrameworks = frameworkPair.Item1.Count == 0
					? frameworkGroups.Keys.ToList ()
					: frameworkPair.Item1;

				foreach (var framework in addToFrameworks) {
					List<ProjectRestoreReference> references;
					if (frameworkGroups.TryGetValue (framework, out references)) {
						// Ensure unique
						if (!references
							.Any(e => e.ProjectUniqueName
								.Equals (frameworkPair.Item2.ProjectUniqueName, StringComparison.OrdinalIgnoreCase))) {
							references.Add (frameworkPair.Item2);
						}
					}
				}
			}

			// Add groups to spec
			foreach (var frameworkPair in frameworkGroups) {
				spec.RestoreMetadata.TargetFrameworks.Add (new ProjectRestoreMetadataFrameworkInfo (frameworkPair.Key) {
					ProjectReferences = frameworkPair.Value
				});
			}
		}

		static bool IsProjectReference (ProjectReference projectReference)
		{
			if (projectReference.ReferenceType != ReferenceType.Project)
				return false;

			if (!projectReference.ReferenceOutputAssembly)
				return false;

			if (projectReference.Include != null)
				return !projectReference.Include.EndsWith (".shproj", StringComparison.OrdinalIgnoreCase);

			return false;
		}

		static Tuple<List<NuGetFramework>, ProjectRestoreReference> GetProjectRestoreReference (
			ProjectReference item,
			IDotNetProject project,
			ILogger logger)
		{
			var frameworks = GetFrameworks (project).ToList ();

			var referencedProject = project.ParentSolution.ResolveProject (item);
			if (referencedProject == null) {
				logger.LogWarning (GettextCatalog.GetString ("WARNING: Unable to resolve project '{0}' referenced by '{1}'.", item.Include, project.Name));
				return null;
			}

			var reference = new ProjectRestoreReference () {
				ProjectPath = referencedProject.FileName,
				ProjectUniqueName = referencedProject.FileName,
			};

			MSBuildRestoreUtility.ApplyIncludeFlags (
				reference,
				item.Metadata.GetValue ("IncludeAssets"),
				item.Metadata.GetValue ("ExcludeAssets"),
				item.Metadata.GetValue ("PrivateAssets"));

			return new Tuple<List<NuGetFramework>, ProjectRestoreReference> (frameworks, reference);
		}

		static void AddPackageReferences (PackageSpec packageSpec, IDotNetProject project)
		{
			foreach (var packageReference in project.GetPackageReferences ()) {
				var dependency = new LibraryDependency ();

				dependency.AutoReferenced = packageReference.IsImplicit;

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
			MSBuildRestoreUtility.ApplyIncludeFlags (
				dependency,
				packageReference.Metadata.GetValue ("IncludeAssets"),
				packageReference.Metadata.GetValue ("ExcludeAssets"),
				packageReference.Metadata.GetValue ("PrivateAssets"));
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
				frameworks.UnionWith (MSBuildStringUtility.Split (frameworksString));
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

		static void AddPackageTargetFallbacks (PackageSpec packageSpec, IDotNetProject project)
		{
			var packageTargetFallback = GetPackageTargetFallbackList (project)
				.Select (NuGetFramework.Parse)
				.ToList ();

			var assetTargetFallback = GetAssetTargetFallbackList (project)
				.Select (NuGetFramework.Parse)
				.ToList ();

			if (!packageTargetFallback.Any () && !assetTargetFallback.Any ())
				return;

			AssetTargetFallbackUtility.EnsureValidFallback (
				packageTargetFallback,
				assetTargetFallback,
				packageSpec.FilePath);

			var frameworks = GetProjectFrameworks (project);
			foreach (var framework in frameworks) {
				var frameworkInfo = packageSpec.GetTargetFramework (framework);
				AssetTargetFallbackUtility.ApplyFramework (frameworkInfo, packageTargetFallback, assetTargetFallback);
			}
		}

		static IEnumerable<string> GetPackageTargetFallbackList (IDotNetProject project)
		{
			var properties = project.EvaluatedProperties;
			if (properties != null) {
				return MSBuildStringUtility.Split (properties.GetValue ("PackageTargetFallback"));
			}

			return new string[0];
		}

		static IEnumerable<string> GetAssetTargetFallbackList (IDotNetProject project)
		{
			return MSBuildStringUtility.Split (
				project.EvaluatedProperties.GetValue (AssetTargetFallbackUtility.AssetTargetFallback)
			);
		}

		static RuntimeGraph GetRuntimeGraph (IDotNetProject project)
		{
			var runtimes = MSBuildStringUtility.Split (project.EvaluatedProperties.GetValue ("RuntimeIdentifiers"))
				.Distinct (StringComparer.Ordinal)
				.Select (rid => new RuntimeDescription (rid))
				.ToList ();

			var supports = MSBuildStringUtility.Split (project.EvaluatedProperties.GetValue ("RuntimeSupports"))
				.Distinct (StringComparer.Ordinal)
				.Select (s => new CompatibilityProfile(s))
				.ToList ();

			return new RuntimeGraph (runtimes, supports);
		}

		static WarningProperties GetWarningProperties (IDotNetProject project)
		{
			return MSBuildRestoreUtility.GetWarningProperties (
				project.EvaluatedProperties.GetValue ("TreatWarningsAsErrors"),
				project.EvaluatedProperties.GetValue ("WarningsAsErrors"),
				project.EvaluatedProperties.GetValue ("NoWarn")
			);
		}

		static IList<string> GetFallbackPackageFolders (ISettings settings, IDotNetProject project)
		{
			var folders = new List<string> ();

			AddFolders (folders, project, "RestoreFallbackFolders");

			if (folders.Any ()) {
				HandleClear (folders);
			} else {
				folders = SettingsUtility.GetFallbackPackageFolders (settings).ToList ();
			}

			AddFolders (folders, project, "RestoreAdditionalProjectFallbackFolders");

			return folders;
		}

		static void AddFolders (List<string> folders, IDotNetProject project, string propertyName)
		{
			string foldersProperty = project.EvaluatedProperties.GetValue (propertyName);
			if (string.IsNullOrEmpty (foldersProperty))
				return;

			var normalizedFolders = MSBuildStringUtility.Split (foldersProperty)
				.Select (folder => NormalizeFolder (project.BaseDirectory, folder));

			folders.AddRange (normalizedFolders);
		}

		static string NormalizeFolder (FilePath baseDirectory, string folder)
		{
			if (IsClearKeyword (folder))
				return folder;

			return MSBuildProjectService.FromMSBuildPath (baseDirectory, folder);
		}

		static bool IsClearKeyword (string item)
		{
			return StringComparer.OrdinalIgnoreCase.Equals ("clear", item);
		}

		static void HandleClear (List<string> values)
		{
			if (MSBuildRestoreUtility.ContainsClearKeyword (values))
				values.Clear ();
		}

		static string GetPackagesPath (ISettings settings, IDotNetProject project)
		{
			string packagesPath = project.EvaluatedProperties.GetValue ("RestorePackagesPath");
			if (!string.IsNullOrEmpty (packagesPath))
				return MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, packagesPath);

			return SettingsUtility.GetGlobalPackagesFolder (settings);
		}

		static IList<PackageSource> GetSources (ISettings settings, IDotNetProject project)
		{
			var sources = new List<PackageSource> ();

			AddSources (sources, project, "RestoreSources");

			if (sources.Any ()) {
				HandleClear (sources);
			} else {
				sources = SettingsUtility.GetEnabledSources (settings).ToList ();
			}

			AddSources (sources, project, "RestoreAdditionalProjectSources");

			return sources;
		}

		static void AddSources (List<PackageSource> sources, IDotNetProject project, string propertyName)
		{
			string sourcesProperty = project.EvaluatedProperties.GetValue (propertyName);
			if (string.IsNullOrEmpty (sourcesProperty))
				return;

			var additionalPackageSources = MSBuildStringUtility.Split (sourcesProperty)
				.Select (source => CreatePackageSource (project.BaseDirectory, source));

			sources.AddRange (additionalPackageSources);
		}

		static PackageSource CreatePackageSource (FilePath baseDirectory, string source)
		{
			if (!IsClearKeyword (source)) {
				source = MSBuildProjectService.UnescapePath (source);
				source = UriUtility.GetAbsolutePath (baseDirectory, source);
			}

			return new PackageSource (source);
		}

		static void HandleClear (List<PackageSource> sources)
		{
			if (MSBuildRestoreUtility.ContainsClearKeyword (sources.Select (source => source.Source)))
				sources.Clear ();
		}
	}
}
