// ProjectDescriptor.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using MonoDevelop.Projects.MSBuild;


namespace MonoDevelop.Ide.Templates
{
	internal class ProjectDescriptor : ISolutionItemDescriptor, ICustomProjectCIEntry
	{
		private string name;
		private string type;
		private string directory;
		private string createCondition;

		private List<FileDescriptionTemplate> files = new List<FileDescriptionTemplate> ();
		private List<SingleFileDescriptionTemplate> resources = new List<SingleFileDescriptionTemplate> ();
		private List<ProjectReferenceDescription> references = new List<ProjectReferenceDescription> ();

		private XmlElement projectOptions = null;
		private List<ProjectTemplatePackageReference> packageReferences = new List<ProjectTemplatePackageReference> ();

		protected ProjectDescriptor ()
		{
		}

		public static ProjectDescriptor CreateProjectDescriptor (XmlElement xmlElement, FilePath baseDirectory)
		{
			ProjectDescriptor projectDescriptor = new ProjectDescriptor ();

			projectDescriptor.name = xmlElement.GetAttribute ("name");
			projectDescriptor.directory = xmlElement.GetAttribute ("directory");
			projectDescriptor.createCondition = xmlElement.GetAttribute ("if");

			projectDescriptor.type = xmlElement.GetAttribute ("type");

			if (xmlElement ["Files"] != null) {
				foreach (XmlNode xmlNode in xmlElement["Files"].ChildNodes)
					if (xmlNode is XmlElement)
						projectDescriptor.files.Add (
							FileDescriptionTemplate.CreateTemplate ((XmlElement)xmlNode, baseDirectory));
			}

			if (xmlElement ["Resources"] != null) {
				foreach (XmlNode xmlNode in xmlElement["Resources"].ChildNodes) {
					if (xmlNode is XmlElement) {
						var fileTemplate = FileDescriptionTemplate.CreateTemplate ((XmlElement)xmlNode, baseDirectory);
						if (fileTemplate is SingleFileDescriptionTemplate)
							projectDescriptor.resources.Add ((SingleFileDescriptionTemplate)fileTemplate);
						else
							MessageService.ShowError (GettextCatalog.GetString ("Only single-file templates allowed to generate resource files"));
					}

				}
			}

			if (xmlElement ["References"] != null) {
				foreach (XmlNode xmlNode in xmlElement["References"].ChildNodes) {
					projectDescriptor.references.Add (new ProjectReferenceDescription ((XmlElement) xmlNode));
				}
			}

			projectDescriptor.projectOptions = xmlElement ["Options"];
			if (projectDescriptor.projectOptions == null)
				projectDescriptor.projectOptions = xmlElement.OwnerDocument.CreateElement ("Options");

			if (xmlElement ["Packages"] != null) {
				foreach (XmlNode xmlNode in xmlElement["Packages"].ChildNodes) {
					if (xmlNode is XmlElement) {
						var packageReference = ProjectTemplatePackageReference.Create ((XmlElement)xmlNode, baseDirectory);
						projectDescriptor.packageReferences.Add (packageReference);
					}
				}
			}

			return projectDescriptor;
		}

		public SolutionItem CreateItem (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			if (string.IsNullOrEmpty (projectOptions.GetAttribute ("language")) && !string.IsNullOrEmpty (defaultLanguage))
				projectOptions.SetAttribute ("language", defaultLanguage);

			var lang = projectOptions.GetAttribute ("language");
			var splitType = !string.IsNullOrEmpty (type) ? type.Split (new char [] {','}, StringSplitOptions.RemoveEmptyEntries).Select (t => t.Trim()).ToArray() : null;
			var projectTypes = splitType != null ? splitType : new string[] {lang};

			var projectType = projectTypes [0];

			string[] flavors;

			if (!Services.ProjectService.CanCreateSolutionItem (projectType, projectCreateInformation, projectOptions) && projectType != lang && !string.IsNullOrEmpty (lang)) {
				// Maybe the type of the template is just a flavor id. In that case try using the language as project type.
				projectType = lang;
				flavors = splitType ?? new string[0];
			} else
				flavors = projectTypes.Skip (1).ToArray ();

			if (!Services.ProjectService.CanCreateSolutionItem (projectType, projectCreateInformation, projectOptions)) {
				LoggingService.LogError ("Could not create project of type '" + string.Join (",", projectTypes) + "'. Project skipped");
				return null;
			}

			if (!ShouldCreateProject (projectCreateInformation))
				return null;

			Project project = Services.ProjectService.CreateProject (projectType, projectCreateInformation, projectOptions, flavors);
			return project;
		}

		public async void InitializeItem (SolutionFolderItem policyParent, ProjectCreateInformation projectCreateInformation, string defaultLanguage, SolutionItem item)
		{
			MonoDevelop.Projects.Project project = item as MonoDevelop.Projects.Project;

			if (project == null) {
				MessageService.ShowError (GettextCatalog.GetString ("Can't create project with type: {0}", type));
				return;
			}

			// Set the file before setting the name, to make sure the file extension is kept
			project.FileName = Path.Combine (projectCreateInformation.ProjectBasePath, projectCreateInformation.ProjectName);
			project.Name = projectCreateInformation.ProjectName;

			var dnp = project as DotNetProject;
			if (dnp != null) {
				if (policyParent.ParentSolution != null && !policyParent.ParentSolution.FileFormat.SupportsFramework (dnp.TargetFramework)) {
					SetClosestSupportedTargetFramework (policyParent.ParentSolution.FileFormat, dnp);
				}
				var substitution = new string[,] { { "ProjectName", GetProjectNameForSubstitution (projectCreateInformation) } };
				foreach (var desc in references) {
					if (!projectCreateInformation.ShouldCreate (desc.CreateCondition))
						continue;
					var pr = desc.Create ();
					if (pr.ReferenceType == ReferenceType.Project) {
						string referencedProjectName = ReplaceParameters (pr.Reference, substitution, projectCreateInformation);
						var parsedReference = ProjectReference.RenameReference (pr, referencedProjectName);
						dnp.References.Add (parsedReference);
					} else
						dnp.References.Add (pr);
				}
			}

			foreach (SingleFileDescriptionTemplate resourceTemplate in resources) {
				try {
					if (!projectCreateInformation.ShouldCreate (resourceTemplate.CreateCondition))
						continue;
					var projectFile = new ProjectFile (await resourceTemplate.SaveFile (policyParent, project, defaultLanguage, project.BaseDirectory, null));
					projectFile.BuildAction = BuildAction.EmbeddedResource;
					project.Files.Add (projectFile);
				} catch (Exception ex) {
					if (!IdeApp.IsInitialized)
						throw;
					MessageService.ShowError (GettextCatalog.GetString ("File {0} could not be written.", resourceTemplate.Name), ex);
				}
			}

			foreach (FileDescriptionTemplate fileTemplate in files) {
				try {
					if (!projectCreateInformation.ShouldCreate (fileTemplate.CreateCondition))
						continue;
					fileTemplate.SetProjectTagModel (projectCreateInformation.Parameters);
					fileTemplate.AddToProject (policyParent, project, defaultLanguage, project.BaseDirectory, null);
				} catch (Exception ex) {
					if (!IdeApp.IsInitialized)
						throw;
					MessageService.ShowError (GettextCatalog.GetString ("File {0} could not be written.", fileTemplate.Name), ex);
				} finally {
					fileTemplate.SetProjectTagModel (null);
				}
			}
		}

		static string GetProjectNameForSubstitution (ProjectCreateInformation projectCreateInformation)
		{
			var templateInformation = projectCreateInformation as ProjectTemplateCreateInformation;
			if (templateInformation != null) {
				return templateInformation.UserDefinedProjectName;
			}
			return projectCreateInformation.SolutionName;
		}

		static string ReplaceParameters (string input, string[,] substitution, ProjectCreateInformation projectCreateInformation)
		{
			string updatedText = StringParserService.Parse (input, substitution);
			return StringParserService.Parse (updatedText, projectCreateInformation.Parameters);
		}
		
		static void SetClosestSupportedTargetFramework (MSBuildFileFormat format, DotNetProject project)
		{
			// If the solution format can't write this project due to an unsupported framework, try finding the
			// closest valid framework. DOn't worry about whether it's installed, that's up to the user to correct.
			TargetFramework curFx = project.TargetFramework;
			var candidates = Runtime.SystemAssemblyService.GetTargetFrameworks ()
				.Where (fx =>
					//only frameworks with the same ID, else version comparisons are meaningless
					fx.Id.Identifier == curFx.Id.Identifier &&
					//don't consider profiles, only full frameworks
					fx.Id.Profile == null &&
					//and the project and format must support the framework
					project.SupportsFramework (fx) && format.SupportsFramework (fx))
					//FIXME: string comparisons aren't a valid way to compare profiles, but it works w/released .NET versions
				.OrderBy (fx => fx.Id.Version)
				.ToList ();

			TargetFramework newFx =
				candidates.FirstOrDefault (fx => string.CompareOrdinal (fx.Id.Version, curFx.Id.Version) > 0)
				 ?? candidates.LastOrDefault ();

			if (newFx != null)
				project.TargetFramework = newFx;
		}

		public ProjectCreateInformation CreateProjectCI (ProjectCreateInformation projectCI)
		{
			var projectCreateInformation = new ProjectCreateInformation (projectCI);
			var substitution = new string[,] { { "ProjectName", projectCreateInformation.ProjectName } };

			projectCreateInformation.ProjectName = ReplaceParameters (name, substitution, projectCreateInformation);

			if (string.IsNullOrEmpty (directory) || directory == ".")
				return projectCreateInformation;

			string dir = ReplaceParameters (directory, substitution, projectCreateInformation);
			projectCreateInformation.ProjectBasePath = Path.Combine (projectCreateInformation.SolutionPath, dir);

			if (ShouldCreateProject (projectCreateInformation) && !Directory.Exists (projectCreateInformation.ProjectBasePath))
				Directory.CreateDirectory (projectCreateInformation.ProjectBasePath);

			return projectCreateInformation;
		}

		public bool HasPackages ()
		{
			return packageReferences.Any ();
		}

		[Obsolete]
		public IList<ProjectTemplatePackageReference> GetPackageReferences ()
		{
			return packageReferences;
		}

		internal string ProjectType {
			get {
				return type;
			}
		}

		public IList<ProjectTemplatePackageReference> GetPackageReferences (ProjectCreateInformation projectCreateInformation)
		{
			return packageReferences
				.Where (packageReference => projectCreateInformation.ShouldCreate (packageReference.CreateCondition))
				.ToList ();
		}

		public bool ShouldCreateProject (ProjectCreateInformation projectCreateInformation)
		{
			return projectCreateInformation.ShouldCreate (createCondition);
		}

		class ProjectReferenceDescription
		{
			XmlElement elem;

			public ProjectReferenceDescription (XmlElement elem)
			{
				this.elem = elem;
				CreateCondition = elem.GetAttribute ("if");
			}

			public ProjectReference Create ()
			{
				var refType = elem.GetAttribute ("type");
				var projectReference = ProjectReference.CreateCustomReference ((ReferenceType)Enum.Parse (typeof(ReferenceType), refType), elem.GetAttribute ("refto"));
				var hintPath = GetMSBuildReferenceHintPath ();
				if (hintPath != null)
					projectReference.Metadata.SetValue ("HintPath", hintPath);
				
				string specificVersion = elem.GetAttribute ("SpecificVersion");
				if (!string.IsNullOrEmpty (specificVersion))
					projectReference.SpecificVersion = bool.Parse (specificVersion);
				string localCopy = elem.GetAttribute ("LocalCopy");
				if (!string.IsNullOrEmpty (localCopy) && projectReference.CanSetLocalCopy)
					projectReference.LocalCopy = bool.Parse (localCopy);
				string referenceOutputAssembly = elem.GetAttribute ("ReferenceOutputAssembly");
				if (!string.IsNullOrEmpty (referenceOutputAssembly))
					projectReference.ReferenceOutputAssembly = bool.Parse (referenceOutputAssembly);
				return projectReference;
			}

			string GetMSBuildReferenceHintPath ()
			{
				string hintPath = elem.GetAttribute ("HintPath");
				if (!string.IsNullOrEmpty (hintPath))
					return hintPath;
				return null;
			}

			public string CreateCondition { get; private set; }
		}
	}
}