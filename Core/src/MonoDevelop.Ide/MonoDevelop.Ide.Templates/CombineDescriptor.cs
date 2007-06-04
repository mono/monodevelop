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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Templates
{
	internal class CombineDescriptor: ICombineEntryDescriptor
	{
		ArrayList entryDescriptors = new ArrayList();
		
		string name;
		string startupProject    = null;
		string relativeDirectory = null;
		string typeName;
	
		public string StartupProject {
			get {
				return startupProject;
			}
		}

		protected CombineDescriptor (string name, string type)
		{
			this.name = name;
			this.typeName = type;
		}
		
		public ICombineEntryDescriptor[] EntryDescriptors {
			get { return (ICombineEntryDescriptor[]) entryDescriptors.ToArray (typeof(ICombineEntryDescriptor)); }
		}
		
		public string CreateEntry (NewSolutionData projectCreateInformation, string defaultLanguage, ref string guid)
		{
			guid = "";
			Solution newCombine;
			
			if (typeName != null && typeName.Length > 0) {
				Type type = Type.GetType (typeName);
				if (type == null) {
					Services.MessageService.ShowError (GettextCatalog.GetString ("Can't create solution with type: {0}", typeName));
					return String.Empty;
				}
				newCombine = (Solution) Activator.CreateInstance (type);
			} else
				newCombine = new Solution ();

			string  newCombineName = Runtime.StringParserService.Parse(name, new string[,] { 
				{"ProjectName", projectCreateInformation.Name}
			});
			
			string oldCombinePath = projectCreateInformation.Path;
			string oldProjectPath = projectCreateInformation.ProjectPath;
			if (relativeDirectory != null && relativeDirectory.Length > 0 && relativeDirectory != ".") {
				projectCreateInformation.Path        = Path.Combine (projectCreateInformation.Path, relativeDirectory);
				projectCreateInformation.ProjectPath = Path.Combine (projectCreateInformation.Path, relativeDirectory);
				if (!Directory.Exists(projectCreateInformation.Path)) {
					Directory.CreateDirectory(projectCreateInformation.Path);
				}
				if (!Directory.Exists(projectCreateInformation.ProjectPath)) {
					Directory.CreateDirectory(projectCreateInformation.ProjectPath);
				}
			}
			SolutionSection configSection = newCombine.GetSection ("SolutionConfigurationPlatforms");
			if (configSection != null) {
				configSection.Contents.Add(new KeyValuePair<string, string> ("Debug|Any CPU", "Debug|Any CPU"));
				configSection.Contents.Add(new KeyValuePair<string, string> ("Release|Any CPU", "Release|Any CPU"));
			}

			// Create sub projects
			SolutionSection projectConfigSection = newCombine.GetSection ("ProjectConfigurationPlatforms");
			foreach (ICombineEntryDescriptor entryDescriptor in entryDescriptors) {
				string newGuid = "";
				string fileName = entryDescriptor.CreateEntry (projectCreateInformation, defaultLanguage, ref newGuid);
				string deNormalizedFileName = SolutionProject.DeNormalizePath (fileName.Substring (Runtime.FileService.GetDirectoryNameWithSeparator(projectCreateInformation.Path).Length));  
					
				newCombine.AddItem ( new SolutionProject ("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", newGuid, Path.GetFileNameWithoutExtension (fileName), deNormalizedFileName));
				if (configSection != null && projectConfigSection != null) {
					foreach (KeyValuePair<string, string> config in configSection.Contents) {
						projectConfigSection.Contents.Add(new KeyValuePair<string, string> (newGuid + "." + config.Key + ".Build.0", config.Value));
						projectConfigSection.Contents.Add(new KeyValuePair<string, string> (newGuid + "." + config.Key + ".ActiveCfg", config.Value));
					}
				}
			}
			
			projectCreateInformation.Path        = oldCombinePath;
			projectCreateInformation.ProjectPath = oldProjectPath;
			
			// Save combine
			using (IProgressMonitor monitor = new NullProgressMonitor ()) {
				string combineLocation = Path.Combine (projectCreateInformation.Path, newCombineName + ".sln");
				if (File.Exists(combineLocation)) {
					IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
					if (messageService.AskQuestion (GettextCatalog.GetString ("Solution file {0} already exists, do you want to overwrite\nthe existing file?", combineLocation))) {
						newCombine.Save (combineLocation);
					}
				} else {
					newCombine.Save (combineLocation);
				}
				
				//newCombine.Dispose();
				return combineLocation;
			}
			
			/*
			Combine newCombine;
			
			if (typeName != null && typeName.Length > 0) {
				Type type = Type.GetType (typeName);
				if (type == null) {
					Services.MessageService.ShowError (GettextCatalog.GetString ("Can't create solution with type: {0}", typeName));
					return String.Empty;
				}
				newCombine = (Combine) Activator.CreateInstance (type);
			} else
				newCombine = new Combine();

			string  newCombineName = Runtime.StringParserService.Parse(name, new string[,] { 
				{"ProjectName", projectCreateInformation.CombineName}
			});
			
			newCombine.Name = newCombineName;
			
			string oldCombinePath = projectCreateInformation.CombinePath;
			string oldProjectPath = projectCreateInformation.ProjectBasePath;
			if (relativeDirectory != null && relativeDirectory.Length > 0 && relativeDirectory != ".") {
				projectCreateInformation.CombinePath     = projectCreateInformation.CombinePath + Path.DirectorySeparatorChar + relativeDirectory;
				projectCreateInformation.ProjectBasePath = projectCreateInformation.CombinePath + Path.DirectorySeparatorChar + relativeDirectory;
				if (!Directory.Exists(projectCreateInformation.CombinePath)) {
					Directory.CreateDirectory(projectCreateInformation.CombinePath);
				}
				if (!Directory.Exists(projectCreateInformation.ProjectBasePath)) {
					Directory.CreateDirectory(projectCreateInformation.ProjectBasePath);
				}
			}

			// Create sub projects
			foreach (ICombineEntryDescriptor entryDescriptor in entryDescriptors) {
				newCombine.AddEntry (entryDescriptor.CreateEntry (projectCreateInformation, defaultLanguage), null);
			}
			
			projectCreateInformation.CombinePath = oldCombinePath;
			projectCreateInformation.ProjectBasePath = oldProjectPath;
			
			// Save combine
			using (IProgressMonitor monitor = new NullProgressMonitor ()) {
				string combineLocation = Runtime.FileService.GetDirectoryNameWithSeparator(projectCreateInformation.CombinePath) + newCombineName + ".mds";
				if (File.Exists(combineLocation)) {
					IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
					if (messageService.AskQuestion (GettextCatalog.GetString ("Solution file {0} already exists, do you want to overwrite\nthe existing file?", combineLocation))) {
						newCombine.Save (combineLocation, monitor);
					}
				} else {
					newCombine.Save (combineLocation, monitor);
				}
			
				newCombine.Dispose();
				return combineLocation;
			}
			*/
		}
		
		public static CombineDescriptor CreateCombineDescriptor(XmlElement element)
		{
			CombineDescriptor combineDescriptor = new CombineDescriptor(element.GetAttribute ("name"), element.GetAttribute ("type"));
			
			if (element.Attributes["directory"] != null) {
				combineDescriptor.relativeDirectory = element.Attributes["directory"].InnerText;
			}
			
			if (element["Options"] != null && element["Options"]["StartupProject"] != null) {
				combineDescriptor.startupProject = element["Options"]["StartupProject"].InnerText;
			}
			
			foreach (XmlNode node in element.ChildNodes) {
				if (node != null) {
					switch (node.Name) {
						case "Project":
							combineDescriptor.entryDescriptors.Add (ProjectDescriptor.CreateProjectDescriptor((XmlElement)node));
							break;
						case "Combine":
							combineDescriptor.entryDescriptors.Add (CreateCombineDescriptor((XmlElement)node));
							break;
						case "CombineEntry":
							combineDescriptor.entryDescriptors.Add (CombineEntryDescriptor.CreateDescriptor((XmlElement)node));
							break;
					}
				}
			}
			return combineDescriptor;
		}
	}
}
