// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.ProgressMonitoring;

using Project_ = MonoDevelop.Projects.Project;

namespace MonoDevelop.Ide.Templates
{
	/// <summary>
	/// This class is used inside the combine templates for projects.
	/// </summary>
	internal class ProjectDescriptor: ICombineEntryDescriptor
	{
		string name;
		string projectType;
		
		ArrayList files      = new ArrayList(); // contains FileTemplate classes
		ArrayList references = new ArrayList(); 
		ArrayList resources  = new ArrayList ();
		
		XmlElement projectOptions = null;
		
		#region public properties
		public ArrayList Files {
			get {
				return files;
			}
		}

		public ArrayList References {
			get {
				return references;
			}
		}

		public ArrayList Resources {
			get {
				return resources;
			}
		}

		public XmlElement ProjectOptions {
			get {
				return projectOptions;
			}
		}
		#endregion

		protected ProjectDescriptor(string name, string relativePath)
		{
			this.name = name;
		}
		
		public string CreateEntry (MonoDevelop.Projects.ProjectCreateInformation projectCreateInformation, string defaultLanguage, ref string guid)
		{
			StringParserService stringParserService = Runtime.StringParserService;
			
			if (projectOptions.GetAttribute ("language") == "") {
/*				if (defaultLanguage == null || defaultLanguage == "")
					throw new InvalidOperationException ("Language not specified in template");
*/				projectOptions.SetAttribute ("language", defaultLanguage);
			}
			
			//Project_ project = Services.ProjectService.CreateProject (projectType, projectCreateInformation, projectOptions);
			IProject project = new MSBuildProject ();
			
			if (project == null) {
				Services.MessageService.ShowError (GettextCatalog.GetString ("Can't create project with type : {0}", projectType));
				return String.Empty;
			}
			
			project.RootNamespace = projectCreateInformation.ProjectName;
			project.AssemblyName  = projectCreateInformation.ProjectName;
			project.OutputType    = "Exe"; // TODO: put output type in the templates
			
			string newProjectName = stringParserService.Parse(name, new string[,] { 
				{"ProjectName", projectCreateInformation.ProjectName}
			});
			
			project.FileName = Runtime.FileService.GetDirectoryNameWithSeparator(projectCreateInformation.ProjectBasePath) + newProjectName + ".csproj";
			
			// Add References
			foreach (MonoDevelop.Projects.ProjectReference projectReference in references) {
				project.Items.Add (new ReferenceProjectItem (projectReference.Reference));
			}

			foreach (FileDescriptionTemplate file in resources) {
				SingleFileDescriptionTemplate singleFile = file as SingleFileDescriptionTemplate;
				if (singleFile == null)
					throw new InvalidOperationException ("Only single-file templates can be used to generate resource files");

				try {
					string[] fileNames = singleFile.Create (defaultLanguage, project.BasePath, null);
					foreach (string fileName in fileNames) 
						project.Items.Add (new ProjectFile (fileName, FileType.EmbeddedResource));
				} catch (Exception ex) {
					Services.MessageService.ShowError (ex, GettextCatalog.GetString ("File {0} could not be written.", file.Name));
				}
			}
	
			// Add Files
			foreach (FileDescriptionTemplate file in files) {
				try {
					string[] fileNames = file.Create (defaultLanguage, project.BasePath, null);
					foreach (string fileName in fileNames) {
						string deNormalizedFileName = SolutionProject.DeNormalizePath (fileName.Substring (Runtime.FileService.GetDirectoryNameWithSeparator(projectCreateInformation.ProjectBasePath).Length));  
						project.Items.Add (new ProjectFile (deNormalizedFileName, FileType.Compile));
					}
				} catch (Exception ex) {
					Services.MessageService.ShowError (ex, GettextCatalog.GetString ("File {0} could not be written.", file.Name));
				}
			}
			
			// Save project
			
			using (IProgressMonitor monitor = new NullProgressMonitor ()) {
				if (File.Exists (project.FileName)) {
					if (Services.MessageService.AskQuestion (GettextCatalog.GetString (
						"Project file {0} already exists, do you want to overwrite\nthe existing file?", project.FileName),
						 GettextCatalog.GetString ("File already exists"))) {
						project.Save (); // project.FileName, monitor
					}
				} else {
					project.Save ();
				}
			}
			guid = project.Guid;
			return project.FileName;
		}
		
		public static ProjectDescriptor CreateProjectDescriptor(XmlElement element)
		{
			ProjectDescriptor projectDescriptor = new ProjectDescriptor(element.Attributes["name"].InnerText, element.Attributes["directory"].InnerText);
			
			projectDescriptor.projectType = element.GetAttribute ("type");
			if (projectDescriptor.projectType == "") projectDescriptor.projectType = "DotNet";
			
			projectDescriptor.projectOptions = element["Options"];
			if (projectDescriptor.projectOptions == null)
				projectDescriptor.projectOptions = element.OwnerDocument.CreateElement ("Options");
			
			if (element["Files"] != null) {
				foreach (XmlNode node in element["Files"].ChildNodes) {
					XmlElement elem = node as XmlElement;
					if (elem != null)
						projectDescriptor.files.Add (FileDescriptionTemplate.CreateTemplate (elem));
				}
			}
			if (element["Resources"] != null) {
				foreach (XmlNode node in element["Resources"].ChildNodes) {
					XmlElement elem = node as XmlElement;
					if (elem != null)
						projectDescriptor.resources.Add (FileDescriptionTemplate.CreateTemplate (elem));
				}
			}
			if (element["References"] != null) {
				foreach (XmlNode node in element["References"].ChildNodes) {
					if (node != null && node.Name == "Reference") {
						MonoDevelop.Projects.ReferenceType referenceType = (MonoDevelop.Projects.ReferenceType)Enum.Parse(typeof(MonoDevelop.Projects.ReferenceType), node.Attributes["type"].InnerXml);
						MonoDevelop.Projects.ProjectReference projectReference = new MonoDevelop.Projects.ProjectReference (referenceType, node.Attributes["refto"].InnerXml);
						projectDescriptor.references.Add(projectReference);
					}
				}
			}
			return projectDescriptor;
		}
	}
}
