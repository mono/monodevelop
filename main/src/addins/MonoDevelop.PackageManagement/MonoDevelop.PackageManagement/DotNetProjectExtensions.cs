// 
// DotNetProjectExtensions.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	internal static class DotNetProjectExtensions
	{
		public static readonly Guid WebApplication = Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}");
		public static readonly Guid WebSite = Guid.Parse("{E24C65DC-7377-472B-9ABA-BC803B73C61A}");

		public static Func<string, bool> FileExists = File.Exists;

		public static bool IsWebProject(this IDotNetProject project)
		{
			return project.HasProjectType(WebApplication) || project.HasProjectType(WebSite);
		}

		public static bool HasProjectType(this IDotNetProject project, Guid projectTypeGuid)
		{
			foreach (string guid in project.FlavorGuids) {
				if (IsMatch(projectTypeGuid, guid)) {
					return true;
				}
			}
			return false;
		}

		static bool IsMatch(Guid guid, string guidStringToMatch)
		{
			Guid result;
			if (Guid.TryParse(guidStringToMatch, out result)) {
				return guid == result;
			}
			return false;
		}

		public static bool HasPackages (this DotNetProject project)
		{
			var nugetAwareProject = project as INuGetAwareProject;
			if (nugetAwareProject != null)
				return nugetAwareProject.HasPackages ();

			return HasPackages (project.BaseDirectory, project.Name) || project.HasPackageReferences ();
		}

		public static string GetPackagesConfigFilePath (this DotNetProject project)
		{
			return GetPackagesConfigFilePath (project.BaseDirectory, project.Name);
		}

		public static bool HasPackages (this IDotNetProject project)
		{
			return AnyFileExists (GetPossiblePackagesConfigOrProjectJsonFilePaths (project.BaseDirectory, project.Name));
		}

		public static bool HasPackagesConfig (this IDotNetProject project)
		{
			return FileExists (GetPackagesConfigFilePath (project));
		}

		static bool HasPackages (string projectDirectory, string projectName)
		{
			return AnyFileExists (GetPossiblePackagesConfigOrProjectJsonFilePaths (projectDirectory, projectName));
		}

		static bool AnyFileExists (IEnumerable<string> files)
		{
			return files.Any (FileExists);
		}

		static IEnumerable<string> GetPossiblePackagesConfigOrProjectJsonFilePaths (string projectDirectory, string projectName)
		{
			yield return GetNonDefaultProjectPackagesConfigFilePath (projectDirectory, projectName);
			yield return GetDefaultPackagesConfigFilePath (projectDirectory);
			yield return ProjectJsonPathUtilities.GetProjectConfigPath (projectDirectory, projectName);
		}

		static string GetNonDefaultProjectPackagesConfigFilePath (string projectDirectory, string projectName)
		{
			return Path.Combine (projectDirectory, GetNonDefaultProjectPackagesConfigFileName (projectName));
		}

		static string GetNonDefaultProjectPackagesConfigFileName (string projectName)
		{
			return "packages." + projectName.Replace (' ', '_') + ".config";
		}

		static string GetDefaultPackagesConfigFilePath (string projectDirectory)
		{
			return Path.Combine (projectDirectory, NuGet.Configuration.NuGetConstants.PackageReferenceFile);
		}

		public static string GetPackagesConfigFilePath (this IDotNetProject project)
		{
			return GetPackagesConfigFilePath (project.BaseDirectory, project.Name);
		}

		static string GetPackagesConfigFilePath (string projectDirectory, string projectName)
		{
			string nonDefaultPackagesConfigFilePath = GetNonDefaultProjectPackagesConfigFilePath (projectDirectory, projectName);
			if (FileExists (nonDefaultPackagesConfigFilePath)) {
				return nonDefaultPackagesConfigFilePath;
			}
			return GetDefaultPackagesConfigFilePath (projectDirectory);
		}

		public static FilePath GetPackagesFolderPath (this DotNetProject project)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			if (solutionManager == null)
				return FilePath.Null;

			NuGetProject nugetProject = solutionManager.GetNuGetProject (new DotNetProjectProxy (project));
			if (nugetProject == null)
				return FilePath.Null;

			return nugetProject.GetPackagesFolderPath (solutionManager);
		}

		public static IEnumerable<string> GetDotNetCoreTargetFrameworks (this Project project)
		{
			foreach (MSBuildPropertyGroup propertyGroup in project.MSBuildProject.PropertyGroups) {
				string framework = propertyGroup.GetValue ("TargetFramework", null);
				if (framework != null)
					return new [] { framework };

				string frameworks = propertyGroup.GetValue ("TargetFrameworks", null);
				if (frameworks != null)
					return frameworks.Split (';');
			}

			return Enumerable.Empty<string> ();
		}

		public static bool IsDotNetCoreProject (this Project project)
		{
			return project.MSBuildProject.Sdk != null;
		}

		public static bool HasPackageReferences (this DotNetProject project)
		{
			return project.Items.OfType<ProjectPackageReference> ().Any () ||
				project.MSBuildProject.HasEvaluatedPackageReferences ();
		}

		public static ProjectPackageReference GetPackageReference (
			this DotNetProject project,
			PackageIdentity packageIdentity,
			bool matchVersion = true)
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.FirstOrDefault (projectItem => projectItem.Equals (packageIdentity, matchVersion));
		}

		public static bool HasPackageReference (this DotNetProject project, string packageId)
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.Any (projectItem => StringComparer.OrdinalIgnoreCase.Equals (projectItem.Include, packageId));
		}

		public static FilePath GetNuGetAssetsFilePath (this DotNetProject project)
		{
			return project.BaseIntermediateOutputPath.Combine (LockFileFormat.AssetsFileName);
		}

		public static bool NuGetAssetsFileExists (this DotNetProject project)
		{
			string assetsFile = project.GetNuGetAssetsFilePath ();
			return File.Exists (assetsFile);
		}

		public static bool DotNetCoreNuGetMSBuildFilesExist (this DotNetProject project)
		{
			var baseDirectory = project.BaseIntermediateOutputPath;
			string projectFileName = project.FileName.FileName;
			string propsFileName = baseDirectory.Combine (projectFileName + ".nuget.g.props");
			string targetsFileName = baseDirectory.Combine (projectFileName + ".nuget.g.targets");

			return File.Exists (propsFileName) &&
				File.Exists (targetsFileName);
		}
	}
}
