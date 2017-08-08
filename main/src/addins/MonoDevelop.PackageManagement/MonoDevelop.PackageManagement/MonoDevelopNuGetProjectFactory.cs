//
// MonoDevelopNuGetProjectFactory.cs
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

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopNuGetProjectFactory
	{
		ISettings settings;

		public MonoDevelopNuGetProjectFactory ()
			: this (SettingsLoader.LoadDefaultSettings ())
		{
		}

		public MonoDevelopNuGetProjectFactory (ISettings settings)
		{
			this.settings = settings;
		}

		public NuGetProject CreateNuGetProject (IDotNetProject project)
		{
			return CreateNuGetProject (project.DotNetProject);
		}

		public NuGetProject CreateNuGetProject (DotNetProject project)
		{
			return CreateNuGetProject (project, new EmptyNuGetProjectContext ());
		}

		public NuGetProject CreateNuGetProject (IDotNetProject project, INuGetProjectContext context)
		{
			return CreateNuGetProject (project.DotNetProject, context);
		}

		public NuGetProject CreateNuGetProject (DotNetProject project, INuGetProjectContext context)
		{
			Runtime.AssertMainThread ();

			var nugetAwareProject = project as INuGetAwareProject;
			if (nugetAwareProject != null)
				return nugetAwareProject.CreateNuGetProject ();

			NuGetProject dotNetCoreProject = DotNetCoreNuGetProject.Create (project);
			if (dotNetCoreProject != null)
				return dotNetCoreProject;

			NuGetProject packageReferenceProject = PackageReferenceNuGetProject.Create (project);
			if (packageReferenceProject != null)
				return packageReferenceProject;

			var projectSystem = new MonoDevelopMSBuildNuGetProjectSystem (project, context);

			string projectJsonPath = ProjectJsonPathUtilities.GetProjectConfigPath (project.BaseDirectory, project.Name);

			if (File.Exists (projectJsonPath)) {
				return new ProjectJsonBuildIntegratedNuGetProject (
					projectJsonPath,
					project.FileName,
					project,
					settings);
			}

			string baseDirectory = GetBaseDirectory (project);
			string folderNuGetProjectFullPath = PackagesFolderPathUtility.GetPackagesFolderPath (baseDirectory, settings);

			string packagesConfigFolderPath = project.BaseDirectory;

			return new MonoDevelopMSBuildNuGetProject (
				projectSystem, 
				folderNuGetProjectFullPath, 
				packagesConfigFolderPath);
		}

		static string GetBaseDirectory (DotNetProject project)
		{
			if (project.ParentSolution != null)
				return project.ParentSolution.BaseDirectory;

			LoggingService.LogWarning ("Project has no solution. Using project directory as base directory for NuGet. Project: '{0}'", project.FileName);

			return project.BaseDirectory;
		}
	}
}

