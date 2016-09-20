//
// AddPlatformImplementationViewModel.cs
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

using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Projects.SharedAssetsProjects;
using System.Linq;

namespace MonoDevelop.Packaging
{
	class AddPlatformImplementationViewModel
	{
		public bool CreateAndroidProject { get; set; }
		public bool CreateIOSProject { get; set; }
		public bool CreateSharedProject { get; set; }

		public bool IsCreateAndroidProjectEnabled { get; set; }
		public bool IsCreateIOSProjectEnabled { get; set; }
		public bool IsCreateSharedProjectEnabled { get; set; }

		public DotNetProject Project { get; set; }

		SharedAssetsProject sharedProject { get; set; }
		DotNetProject androidProject { get; set; }
		DotNetProject iosProject { get; set; }

		FilePath projectsBaseDirectory;

		public AddPlatformImplementationViewModel (DotNetProject project)
		{
			Project = project;

			ConfigureSettings ();
		}

		void ConfigureSettings ()
		{
			IsCreateAndroidProjectEnabled = Services.ProjectService.CanCreateProject ("C#", "MonoDroid");
			IsCreateIOSProjectEnabled = Services.ProjectService.CanCreateProject ("C#", "XamarinIOS");
			IsCreateSharedProjectEnabled = Services.ProjectService.CanCreateProject ("SharedAssetsProject");
		}

		public bool AnyItemsToCreate ()
		{
			return CreateAndroidProject || CreateIOSProject;
		}

		public async Task CreateProjects (ProgressMonitor monitor)
		{
			if (CreateSharedProject) {
				await CreateNewSharedProject (monitor);
			}

			if (CreateAndroidProject) {
				await CreateNewAndroidProject (monitor);
			}

			if (CreateIOSProject) {
				await CreateNewIOSProject (monitor);
			}

			await CreateNuGetPackagingProject (monitor);

			if (CreateSharedProject) {
				await MigrateFiles (monitor);
			}

			await Project.ParentSolution.SaveAsync (monitor);
		}

		async Task CreateNewSharedProject (ProgressMonitor monitor)
		{
			sharedProject = new SharedAssetsProject ("C#");
			sharedProject.FileName = GetNewProjectFileName ("SharedAssetsProject");
			sharedProject.DefaultNamespace = Project.DefaultNamespace;
			Project.ParentFolder.AddItem (sharedProject);

			await SaveProject (monitor, sharedProject);

			await Project.ParentSolution.SaveAsync (monitor);

			AddProjectReference (Project, sharedProject);

			await Project.SaveAsync (monitor);
		}

		async Task CreateNewAndroidProject (ProgressMonitor monitor)
		{
			androidProject = await CreateNewProject (monitor, "MonoDroid", true) as DotNetProject;

			if (sharedProject != null)
				AddProjectReference (androidProject, sharedProject);
		}

		async Task CreateNewIOSProject (ProgressMonitor monitor)
		{
			iosProject = await CreateNewProject (monitor, "XamarinIOS", true) as DotNetProject;

			if (sharedProject != null)
				AddProjectReference (iosProject, sharedProject);
		}

		async Task MigrateFiles (ProgressMonitor monitor)
		{
			var migrator = new ProjectFileMigrator ();
			migrator.MigrateFiles (Project, sharedProject);

			await SaveProject (monitor, Project);
			await SaveProject (monitor, sharedProject);
		}

		async Task<DotNetProject> CreateNewProject (ProgressMonitor monitor, string projectType, bool addAssemblyInfo = false)
		{
			FilePath projectFileName = GetNewProjectFileName (projectType);
			var createInfo = CreateProjectCreateInformation (projectFileName);
			var options = CreateProjectOptions ();

			var newProject = Services.ProjectService.CreateProject ("C#", createInfo, options, projectType) as DotNetProject;

			newProject.FileName = projectFileName;
			newProject.DefaultNamespace = Project.DefaultNamespace;
			newProject.SetOutputAssemblyName (GetOutputAssemblyName ());

			if (addAssemblyInfo)
				AddAssemblyInfoFile (newProject);

			Project.ParentFolder.AddItem (newProject);

			await SaveProject (monitor, newProject);

			return newProject;
		}

		string GetOutputAssemblyName ()
		{
			var config = Project.Configurations.OfType<DotNetProjectConfiguration> ().FirstOrDefault ();
			if (config != null) {
				return config.OutputAssembly;
			}

			return Project.Name;
		}

		ProjectCreateInformation CreateProjectCreateInformation (FilePath projectFileName)
		{
			return new ProjectCreateInformation {
				ProjectBasePath = projectFileName.ParentDirectory,
				SolutionPath = Project.ParentSolution.BaseDirectory,
				ProjectName = projectFileName.FileNameWithoutExtension
			};
		}

		XmlElement CreateProjectOptions ()
		{
			var doc = new XmlDocument ();
			doc.LoadXml ("<Options Target='Library' />");
			return doc.DocumentElement;
		}

		void AddAssemblyInfoFile (Project project)
		{
			var doc = new XmlDocument ();
			doc.LoadXml ("<FileTemplateReference TemplateID='CSharpAssemblyInfo' name='AssemblyInfo.cs' />");
			var fileTemplate = new FileTemplateReference ();
			fileTemplate.Load (doc.DocumentElement, null);

			fileTemplate.AddToProject (project.ParentFolder, project, "C#", project.BaseDirectory, "");
		}

		Task SaveProject (ProgressMonitor monitor, Project project)
		{
			Directory.CreateDirectory (project.BaseDirectory);
			return project.SaveAsync (monitor);
		}

		FilePath GetNewProjectFileName (string projectType)
		{
			string projectNameSuffix = GetProjectNameSuffix (projectType);
			string fileExtension = GetProjectFileExtension (projectType);
			FilePath projectDirectory = ProjectsBaseDirectory.Combine (Project.Name + "." + projectNameSuffix);
			return projectDirectory.Combine (string.Format ("{0}.{1}{2}", Project.Name, projectNameSuffix, fileExtension));
		}

		FilePath ProjectsBaseDirectory {
			get {
				if (projectsBaseDirectory.IsNull) {
					if (Project.BaseDirectory == Project.ParentSolution.BaseDirectory)
						projectsBaseDirectory = Project.ParentSolution.BaseDirectory;
					else
						projectsBaseDirectory = Project.BaseDirectory.ParentDirectory;
				}

				return projectsBaseDirectory;
			}
		}

		static string GetProjectNameSuffix (string projectType)
		{
			switch (projectType) {
				case "MonoDroid":
				return "Android";

				case "XamarinIOS":
				return "iOS";

				case "SharedAssetsProject":
				return "Shared";

				case "NuGetPackaging":
				return "NuGet";

				default:
				return projectType;
			}
		}

		static string GetProjectFileExtension (string projectType)
		{
			if (projectType == "SharedAssetsProject")
				return ".shproj";

			if (projectType == "NuGetPackaging")
				return ".nuproj";

			return ".csproj";
		}

		async Task CreateNuGetPackagingProject (ProgressMonitor monitor)
		{
			FilePath projectFileName = GetNewProjectFileName ("NuGetPackaging");
			var packagingProject = Services.ProjectService.CreateProject ("NuGetPackaging") as PackagingProject;
			packagingProject.FileName = projectFileName;

			var createInfo = CreateProjectCreateInformation (projectFileName);
			packagingProject.InitializeFromTemplate (createInfo, CreateProjectOptions ());

			var moniker = new TargetFrameworkMoniker (TargetFrameworkMoniker.ID_NET_FRAMEWORK, "4.5", null);
			packagingProject.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (moniker);

			MoveNuGetPackageMetadataToPackagingProject (packagingProject);

			await Project.SaveAsync (monitor);

			Project.ParentFolder.AddItem (packagingProject);
			AddNuGetPackagingProjectReferences (packagingProject);

			await SaveProject (monitor, packagingProject);
		}

		void AddNuGetPackagingProjectReferences (PackagingProject packagingProject)
		{
			AddProjectReference (packagingProject, Project);

			if (androidProject != null)
				AddProjectReference (packagingProject, androidProject);

			if (iosProject != null)
				AddProjectReference (packagingProject, iosProject);
		}

		void AddProjectReference (DotNetProject project, Project projectToBeReferenced)
		{
			project.References.Add (ProjectReference.CreateProjectReference (projectToBeReferenced));
		}

		void MoveNuGetPackageMetadataToPackagingProject (PackagingProject packagingProject)
		{
			var metadata = new NuGetPackageMetadata ();
			metadata.Load (Project);
			packagingProject.UpdatePackageMetadata (metadata);

			// Remove NuGet package metadata from original project.
			metadata = new NuGetPackageMetadata ();
			metadata.UpdateProject (Project);
		}
	}
}
