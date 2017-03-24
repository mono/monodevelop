//
// DotNetCoreProjectTemplateWizard.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore.Templating
{
	class DotNetCoreProjectTemplateWizard : TemplateWizard
	{
		bool createDotNetCoreProject;

		public override WizardPage GetPage (int pageNumber)
		{
			return null;
		}

		public override int TotalPages {
			get { return 0; }
		}

		public override string Id {
			get { return "MonoDevelop.DotNetCore.ProjectTemplateWizard"; }
		}

		public override void ConfigureWizard ()
		{
			createDotNetCoreProject = !Parameters.GetBoolValue ("CreateSolution");
			Parameters["CreateDotNetCoreProject"] = createDotNetCoreProject.ToString ();
		}

		public override async void ItemsCreated (IEnumerable<IWorkspaceFileObject> items)
		{
			try {
				using (var progressMonitor = CreateProgressMonitor ()) {
					await CreateDotNetCoreProject (items).ConfigureAwait (false);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to create project.", ex);
			}
		}

		ProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Creating project..."),
				Stock.StatusSolutionOperation,
				false,
				false,
				false);
		}

		Task CreateDotNetCoreProject (IEnumerable<IWorkspaceFileObject> items)
		{
			Solution solution = GetCreatedSolution (items);
			if (solution != null) {
				if (IsWebProject ()) {
					SolutionFolder srcFolder = AddSolutionFoldersToSolution (solution);
					return CreateProject (solution, srcFolder);
				} else {
					return CreateProject (solution);
				}
			}

			DotNetProject project = GetCreatedProject (items);
			solution = project.ParentSolution;
			RemoveProjectFromSolution (project);

			if (IsWebProject ()) {
				SolutionFolder existingSrcFolder = GetSrcSolutionFolder (solution);
				return CreateProject (solution, existingSrcFolder, false);
			} else {
				return CreateProject (solution, false);
			}
		}

		bool IsWebProject ()
		{
			return Parameters.GetBoolValue ("IsWebProject");
		}

		SolutionFolder GetSrcSolutionFolder (Solution solution)
		{
			return solution.RootFolder.Items.OfType<SolutionFolder> ()
				.FirstOrDefault (item => item.Name == "src");
		}

		static Solution GetCreatedSolution (IEnumerable<IWorkspaceFileObject> items)
		{
			if (items.Count () == 1) {
				return items.OfType<Solution> ().FirstOrDefault ();
			}
			return null;
		}

		static DotNetProject GetCreatedProject (IEnumerable<IWorkspaceFileObject> items)
		{
			return items.OfType<DotNetProject> ()
				.FirstOrDefault ();
		}

		Task CreateProject (Solution solution, bool newSolution = true)
		{
			return CreateProject (solution, solution.RootFolder, newSolution);
		}

		async Task<DotNetProject> CreateProject (Solution solution, string projectName)
		{
			string projectTemplateName = Parameters["Template"];
			FilePath templateDirectory = FileTemplateProcessor.GetTemplateDirectory (projectTemplateName);
			FilePath templateFileName = templateDirectory.Combine (projectTemplateName + ".csproj");

			string projectFileName = GenerateNewProjectFileName (solution, projectName);
			var msbuildProject = new MSBuildProject ();
			msbuildProject.Load (templateFileName);

			UpdatePackageReferenceVersions (msbuildProject);

			string projectDirectory = Path.GetDirectoryName (projectFileName);
			Directory.CreateDirectory (projectDirectory);
			msbuildProject.Save (projectFileName);

			// Create files here so they are loaded from wildcards when the project is loaded.
			CreateFilesFromTemplate (projectDirectory, projectName, solution);

			return await IdeApp.Services.ProjectService.ReadSolutionItem (new ProgressMonitor (), projectFileName) as DotNetProject;
		}

		string GenerateNewProjectFileName (Solution solution, string projectName)
		{
			string subDirectory = Path.Combine (projectName, projectName + ".csproj");
			if (IsWebProject ())
				return solution.BaseDirectory.Combine ("src", subDirectory);

			return solution.BaseDirectory.Combine (subDirectory);
		}

		void UpdatePackageReferenceVersions (MSBuildProject project)
		{
			foreach (MSBuildItem packageReference in project.GetAllItems ().Where (item => item.Name == "PackageReference")) {
				string version = packageReference.Metadata.GetValue ("Version");
				if (version != null) {
					version = StringParserService.Parse (version, Parameters);
					packageReference.Metadata.SetValue ("Version", version);
				}
			}
		}

		void CreateFilesFromTemplate (string projectDirectory, string projectName, Solution solution)
		{
			string projectTemplateName = Parameters["Template"];

			string[] files = Parameters["Files"].Split ('|');

			var project = new DummyProject (projectName) {
				BaseDirectory = projectDirectory
			};
			FileTemplateProcessor.CreateFilesFromTemplate (project, solution.RootFolder, projectTemplateName, files);
		}

		void RemoveProjectFromSolution (DotNetProject project)
		{
			project.ParentFolder.Items.Remove (project);
			project.Dispose ();
		}

		SolutionFolder AddSolutionFoldersToSolution (Solution solution)
		{
			return solution.AddSolutionFolder ("src");
		}

		async Task CreateProject (Solution solution, SolutionFolder srcFolder, bool newSolution = true)
		{
			string projectName = Parameters["UserDefinedProjectName"];
			DotNetProject project = await CreateProject (solution, projectName);
			srcFolder.AddItem (project);

			if (newSolution) {
				solution.StartupItem = project;
				solution.GenerateDefaultProjectConfigurations (project);
			} else {
				solution.EnsureConfigurationHasBuildEnabled (project);
			}

			UpdateDefaultRunConfiguration (project);

			if (Parameters.GetBoolValue ("CreateWebRoot")) {
				FilePath webRootDirectory = project.BaseDirectory.Combine ("wwwroot");
				Directory.CreateDirectory (webRootDirectory);
			}

			if (IsWebProject ())
				RemoveProjectDirectoryCreatedByNewProjectDialog (solution.BaseDirectory, projectName);

			await IdeApp.ProjectOperations.SaveAsync (solution);

			OpenProjectFile (project);

			RestorePackages (project);
		}

		void UpdateDefaultRunConfiguration (DotNetProject project)
		{
			if (!Parameters.GetBoolValue ("ExternalConsole"))
				return;

			var runConfig = project.GetDefaultRunConfiguration () as ProcessRunConfiguration;
			if (runConfig != null)
				runConfig.ExternalConsole = true;
		}

		void RemoveProjectDirectoryCreatedByNewProjectDialog (FilePath parentDirectory, string projectName)
		{
			FilePath projectDirectory = parentDirectory.Combine (projectName);
			EmptyDirectoryRemover.Remove (projectDirectory);
		}

		void OpenProjectFile (DotNetProject project)
		{
			FilePath fileName = project.BaseDirectory.Combine (Parameters["OpenFile"]);
			IdeApp.Workbench.OpenDocument (fileName, project, true);
		}

		void RestorePackages (DotNetProject project)
		{
			ProgressMonitorStatusMessage message = ProgressMonitorStatusMessageFactory.CreateRestoringPackagesInProjectMessage ();
			var action = new RestoreNuGetPackagesInDotNetCoreProject (project);
			action.ReloadProject = true;
			PackageManagementServices.BackgroundPackageActionRunner.Run (message, action);
		}
	}
}
