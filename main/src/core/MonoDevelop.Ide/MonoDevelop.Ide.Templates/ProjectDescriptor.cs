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


namespace MonoDevelop.Ide.Templates
{
	internal class ProjectDescriptor : ISolutionItemDescriptor, ICustomProjectCIEntry
	{
		private string name;
		private string type;
		private string directory;

		private List<FileDescriptionTemplate> files = new List<FileDescriptionTemplate> ();
		private List<SingleFileDescriptionTemplate> resources = new List<SingleFileDescriptionTemplate> ();
		private List<ProjectReference> references = new List<ProjectReference> ();

		private XmlElement projectOptions = null;


		protected ProjectDescriptor ()
		{
		}

		public static ProjectDescriptor CreateProjectDescriptor (XmlElement xmlElement, FilePath baseDirectory)
		{
			ProjectDescriptor projectDescriptor = new ProjectDescriptor ();

			projectDescriptor.name = xmlElement.GetAttribute ("name");
			projectDescriptor.directory = xmlElement.GetAttribute ("directory");

			projectDescriptor.type = xmlElement.GetAttribute ("type");
			if (String.IsNullOrEmpty (projectDescriptor.type))
				projectDescriptor.type = "DotNet";

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
					XmlElement elem = (XmlElement)xmlNode;
					var refType = elem.GetAttribute ("type");
					ProjectReference projectReference = new ProjectReference ((ReferenceType)Enum.Parse (typeof(ReferenceType), refType), elem.GetAttribute ("refto"));
					string specificVersion = elem.GetAttribute ("SpecificVersion");
					if (!string.IsNullOrEmpty (specificVersion))
						projectReference.SpecificVersion = bool.Parse (specificVersion);
					string localCopy = elem.GetAttribute ("LocalCopy");
					if (!string.IsNullOrEmpty (localCopy) && projectReference.CanSetLocalCopy)
						projectReference.LocalCopy = bool.Parse (localCopy);
					projectDescriptor.references.Add (projectReference);
				}
			}

			projectDescriptor.projectOptions = xmlElement ["Options"];
			if (projectDescriptor.projectOptions == null)
				projectDescriptor.projectOptions = xmlElement.OwnerDocument.CreateElement ("Options");

			return projectDescriptor;
		}

		public SolutionEntityItem CreateItem (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			if (string.IsNullOrEmpty (projectOptions.GetAttribute ("language")) && !string.IsNullOrEmpty (defaultLanguage))
				projectOptions.SetAttribute ("language", defaultLanguage);

			Project project = Services.ProjectService.CreateProject (type, projectCreateInformation, projectOptions);
			return project;
		}

		public void InitializeItem (SolutionItem policyParent, ProjectCreateInformation projectCreateInformation, string defaultLanguage, SolutionEntityItem item)
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
				var substitution = new string[,] { { "ProjectName", projectCreateInformation.SolutionName } };
				foreach (ProjectReference projectReference in references) {
					if (projectReference.ReferenceType == ReferenceType.Project) {
						string referencedProjectName = StringParserService.Parse (projectReference.Reference, substitution);
						var parsedReference = ProjectReference.RenameReference (projectReference, referencedProjectName);
						dnp.References.Add (parsedReference);
					} else
						dnp.References.Add (projectReference);
				}
			}

			foreach (SingleFileDescriptionTemplate resourceTemplate in resources) {
				try {
					ProjectFile projectFile = new ProjectFile (resourceTemplate.SaveFile (policyParent, project, defaultLanguage, project.BaseDirectory, null));
					projectFile.BuildAction = BuildAction.EmbeddedResource;
					project.Files.Add (projectFile);
				} catch (Exception ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("File {0} could not be written.", resourceTemplate.Name));
					LoggingService.LogError (GettextCatalog.GetString ("File {0} could not be written.", resourceTemplate.Name), ex);
				}
			}

			foreach (FileDescriptionTemplate fileTemplate in files) {
				try {
					fileTemplate.AddToProject (policyParent, project, defaultLanguage, project.BaseDirectory, null);
				} catch (Exception ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("File {0} could not be written.", fileTemplate.Name));
					LoggingService.LogError (GettextCatalog.GetString ("File {0} could not be written.", fileTemplate.Name), ex);
				}
			}
		}
		
		static void SetClosestSupportedTargetFramework (FileFormat format, DotNetProject project)
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
			var projectCreateInformation = projectCI;
			var substitution = new string[,] { { "ProjectName", projectCreateInformation.ProjectName } };

			projectCreateInformation.ProjectName = StringParserService.Parse (name, substitution);

			if (string.IsNullOrEmpty (directory) || directory == ".")
				return projectCreateInformation;

			string dir = StringParserService.Parse (directory, substitution);
			projectCreateInformation.ProjectBasePath = Path.Combine (projectCreateInformation.SolutionPath, dir);

			if (!Directory.Exists (projectCreateInformation.ProjectBasePath))
				Directory.CreateDirectory (projectCreateInformation.ProjectBasePath);

			return projectCreateInformation;
		}
	}
}