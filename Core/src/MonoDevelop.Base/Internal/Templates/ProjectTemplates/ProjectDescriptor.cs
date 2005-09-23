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
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;

using Project_ = MonoDevelop.Internal.Project.Project;

namespace MonoDevelop.Internal.Templates
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
		ArrayList resources = new ArrayList ();
		
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
		
		public string CreateEntry (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			StringParserService stringParserService = Runtime.StringParserService;
			FileUtilityService fileUtilityService = Runtime.FileUtilityService;
			
			if (projectOptions.GetAttribute ("language") == "") {
				if (defaultLanguage == null || defaultLanguage == "")
					throw new InvalidOperationException ("Language not specified in template");
				projectOptions.SetAttribute ("language", defaultLanguage);
			}
			
			Project_ project = Runtime.ProjectService.CreateProject (projectType, projectCreateInformation, projectOptions);
			
			if (project == null) {
				Runtime.MessageService.ShowError(String.Format (GettextCatalog.GetString ("Can't create project with type : {0}"), projectType));
				return String.Empty;
			}
			
			string newProjectName = stringParserService.Parse(name, new string[,] { 
				{"ProjectName", projectCreateInformation.ProjectName}
			});
			
			project.Name = newProjectName;
			
			// Add References
			foreach (ProjectReference projectReference in references) {
				project.ProjectReferences.Add(projectReference);
			}

			foreach (FileDescriptionTemplate file in resources) {
				string fileName = fileUtilityService.GetDirectoryNameWithSeparator(projectCreateInformation.ProjectBasePath) + stringParserService.Parse(file.Name, new string[,] { {"ProjectName", projectCreateInformation.ProjectName} });
				
				ProjectFile resource = new ProjectFile (fileName);
				resource.BuildAction = BuildAction.EmbedAsResource;
				project.ProjectFiles.Add(resource);
				
				if (File.Exists(fileName)) {
					if (!Runtime.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("File {0} already exists, do you want to overwrite\nthe existing file ?"), fileName), GettextCatalog.GetString ("File already exists"))) {
						continue;
					}
				}
				
				try {
					if (!Directory.Exists(Path.GetDirectoryName(fileName))) {
						Directory.CreateDirectory(Path.GetDirectoryName(fileName));
					}
					StreamWriter sr = File.CreateText(fileName);
					sr.Write(stringParserService.Parse(file.Content, new string[,] { {"ProjectName", projectCreateInformation.ProjectName}, {"FileName", fileName}}));
					sr.Close();
				} catch (Exception ex) {
					Runtime.MessageService.ShowError(ex, String.Format (GettextCatalog.GetString ("File {0} could not be written."), fileName));
				}
			}
	
			// Add Files
			foreach (FileDescriptionTemplate file in files) {
				string fileName = fileUtilityService.GetDirectoryNameWithSeparator(projectCreateInformation.ProjectBasePath) + stringParserService.Parse(file.Name, new string[,] { {"ProjectName", projectCreateInformation.ProjectName} });
				
				project.ProjectFiles.Add(new ProjectFile(fileName));
				
				if (File.Exists(fileName)) {
					if (!Runtime.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("File {0} already exists, do you want to overwrite\nthe existing file ?"), fileName), GettextCatalog.GetString ("File already exists"))) {
						continue;
					}
				}
				
				try {
					if (!Directory.Exists(Path.GetDirectoryName(fileName))) {
						Directory.CreateDirectory(Path.GetDirectoryName(fileName));
					}
					StreamWriter sr = File.CreateText(fileName);
					sr.Write(stringParserService.Parse(file.Content, new string[,] { {"ProjectName", projectCreateInformation.ProjectName}, {"FileName", fileName}}));
					sr.Close();
				} catch (Exception ex) {
					Runtime.MessageService.ShowError(ex, String.Format (GettextCatalog.GetString ("File {0} could not be written."), fileName));
				}
			}
			
			// Save project
			string projectLocation = fileUtilityService.GetDirectoryNameWithSeparator(projectCreateInformation.ProjectBasePath) + newProjectName + ".mdp";
			
			
			using (IProgressMonitor monitor = Runtime.TaskService.GetSaveProgressMonitor ()) {
				if (File.Exists(projectLocation)) {
					if (Runtime.MessageService.AskQuestion(String.Format (GettextCatalog.GetString ("Project file {0} already exists, do you want to overwrite\nthe existing file ?"), projectLocation),  GettextCatalog.GetString ("File already exists"))) {
						project.Save (projectLocation, monitor);
					}
				} else {
					project.Save (projectLocation, monitor);
				}
			}
			
			return projectLocation;
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
					if (node != null && node.Name == "File") {
						projectDescriptor.files.Add(new FileDescriptionTemplate(node.Attributes["name"].InnerText, node.InnerText));
					}
				}
			}
			if (element["Resources"] != null) {
				foreach (XmlNode node in element["Resources"].ChildNodes) {
					if (node != null && node.Name == "File") {
						projectDescriptor.resources.Add (new FileDescriptionTemplate (node.Attributes["name"].InnerText, node.InnerText));
					}
				}
			}
			if (element["References"] != null) {
				foreach (XmlNode node in element["References"].ChildNodes) {
					if (node != null && node.Name == "Reference") {
						ProjectReference projectReference = new ProjectReference();
						
						projectReference.ReferenceType = (ReferenceType)Enum.Parse(typeof(ReferenceType), node.Attributes["type"].InnerXml);
						projectReference.Reference     = node.Attributes["refto"].InnerXml;
						projectDescriptor.references.Add(projectReference);
					}
				}
			}
			return projectDescriptor;
		}
	}
}
