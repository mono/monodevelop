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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core.Gui;
using System.IO;
using Gtk;
using System.Collections.Generic;
using System.Xml;


namespace MonoDevelop.Ide.Templates
{
	internal class ProjectDescriptor : ISolutionItemDescriptor
	{
		private string name;
		private string type;

		private List<FileDescriptionTemplate> files = new List<FileDescriptionTemplate> ();
		private List<SingleFileDescriptionTemplate> resources = new List<SingleFileDescriptionTemplate> ();
		private List<ProjectReference> references = new List<ProjectReference> ();

		private XmlElement projectOptions = null;


		protected ProjectDescriptor ()
		{
		}

		public static ProjectDescriptor CreateProjectDescriptor (XmlElement xmlElement)
		{
			ProjectDescriptor projectDescriptor = new ProjectDescriptor ();

			projectDescriptor.name = xmlElement.GetAttribute ("name");

			projectDescriptor.type = xmlElement.GetAttribute ("type");
			if (String.IsNullOrEmpty (projectDescriptor.type))
				projectDescriptor.type = "DotNet";

			if (xmlElement["Files"] != null) {
				foreach (XmlNode xmlNode in xmlElement["Files"].ChildNodes)
					if (xmlNode is XmlElement)
						projectDescriptor.files.Add (FileDescriptionTemplate.CreateTemplate ((XmlElement)xmlNode));
			}

			if (xmlElement["Resources"] != null) {
				foreach (XmlNode xmlNode in xmlElement["Resources"].ChildNodes) {
					if (xmlNode is XmlElement) {
						FileDescriptionTemplate fileTemplate = FileDescriptionTemplate.CreateTemplate ((XmlElement)xmlNode);
						if (fileTemplate is SingleFileDescriptionTemplate)
							projectDescriptor.resources.Add ((SingleFileDescriptionTemplate)fileTemplate); else
							MessageService.ShowError (GettextCatalog.GetString ("Only single-file templates allowed to generate resource files"));
					}

				}
			}

			if (xmlElement["References"] != null) {
				foreach (XmlNode xmlNode in xmlElement["References"].ChildNodes) {
					XmlElement elem = (XmlElement)xmlNode;
					ProjectReference projectReference = new ProjectReference ((ReferenceType)Enum.Parse (typeof(ReferenceType), elem.GetAttribute ("type")), elem.GetAttribute ("refto"));
					string specificVersion = elem.GetAttribute ("SpecificVersion");
					if (!string.IsNullOrEmpty (specificVersion))
						projectReference.SpecificVersion = bool.Parse (specificVersion); else {
						// If SpecificVersion is not specified, then make sure the reference is
						// valid for the default runtime
						if (projectReference.ReferenceType == ReferenceType.Gac) {
							string newRef = IdeApp.Workspace.ActiveRuntime.AssemblyContext.FindInstalledAssembly (projectReference.Reference, null, IdeApp.Services.ProjectService.DefaultTargetFramework);
							if (newRef != projectReference.Reference && newRef != null)
								projectReference = new ProjectReference (ReferenceType.Gac, newRef);
						}
					}

					projectDescriptor.references.Add (projectReference);
				}
			}

			projectDescriptor.projectOptions = xmlElement["Options"];
			if (projectDescriptor.projectOptions == null)
				projectDescriptor.projectOptions = xmlElement.OwnerDocument.CreateElement ("Options");

			return projectDescriptor;
		}

		public SolutionEntityItem CreateItem (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			if ((projectOptions.Attributes["language"] == null) || String.IsNullOrEmpty (projectOptions.Attributes["language"].Value))
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

			string pname = StringParserService.Parse (name, new string[,] { {
				"ProjectName",
				projectCreateInformation.ProjectName
			} });
			
			// Set the file before setting the name, to make sure the file extension is kept
			project.FileName = Path.Combine (projectCreateInformation.ProjectBasePath, pname);
			project.Name = pname;
			
			if (project is DotNetProject) {
				if (policyParent.ParentSolution != null && !policyParent.ParentSolution.FileFormat.CanWrite (item))
					TryFixingFramework (policyParent.ParentSolution.FileFormat, (DotNetProject)project);

				foreach (ProjectReference projectReference in references)
					((DotNetProject)project).References.Add (projectReference);
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

		public void TryFixingFramework (FileFormat format, DotNetProject item)
		{
			// If the solution format can't write this project it may be due to an unsupported
			// framework. Try finding a compatible framework.

			TargetFramework curFx = item.TargetFramework;
			foreach (TargetFramework fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				item.TargetFramework = fx;
				if (format.CanWrite (item))
					return;
			}
			item.TargetFramework = curFx;
		}
	}
}
