//  CombineDescriptor.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Templates
{
	internal class CombineDescriptor
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
		
		public ISolutionItemDescriptor[] EntryDescriptors {
			get { return (ISolutionItemDescriptor[]) entryDescriptors.ToArray (typeof(ISolutionItemDescriptor)); }
		}
		
		public WorkspaceItem CreateEntry (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
		{
			WorkspaceItem item;

			if (typeName != null && typeName.Length > 0) {
				Type type = Type.GetType (typeName);
				if (type == null || !typeof(WorkspaceItem).IsAssignableFrom (type)) {
					MessageService.ShowError (GettextCatalog.GetString ("Can't create solution with type: {0}", typeName));
					return null;
				}
				item = (WorkspaceItem) Activator.CreateInstance (type);
			} else
				item = new Solution ();
			
			string  newCombineName = StringParserService.Parse(name, new string[,] { 
				{"ProjectName", projectCreateInformation.CombineName}
			});
			
			item.Name = newCombineName;
			
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

			Solution sol = item as Solution;
			if (sol != null) {
				List<string> configs = new List<string> ();
				
				// Create sub projects
				foreach (ISolutionItemDescriptor entryDescriptor in entryDescriptors) {
					SolutionItem sit = entryDescriptor.CreateEntry (projectCreateInformation, defaultLanguage);
					sol.RootFolder.Items.Add (sit);
					if (sit is IConfigurationTarget) {
						foreach (ItemConfiguration c in ((IConfigurationTarget)sit).Configurations) {
							if (!configs.Contains (c.Id))
								configs.Add (c.Id);
						}
					}
				}
				
				// Create configurations
				foreach (string conf in configs)
					sol.AddConfiguration (conf, true);
			}
			
			projectCreateInformation.CombinePath = oldCombinePath;
			projectCreateInformation.ProjectBasePath = oldProjectPath;
			item.FileName = Path.Combine (projectCreateInformation.CombinePath, newCombineName);
			
			return item;
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
						case "SolutionItem":
							combineDescriptor.entryDescriptors.Add (SolutionItemDescriptor.CreateDescriptor((XmlElement)node));
							break;
					}
				}
			}
			return combineDescriptor;
		}
	}
}
