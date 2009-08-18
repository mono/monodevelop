//  SolutionDescriptor.cs
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
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using System.Xml;

namespace MonoDevelop.Ide.Templates
{
    internal class SolutionDescriptor 
	{
        string startupProject;
        string directory;
        string name;
        string type;

        private List<ISolutionItemDescriptor> entryDescriptors = new List<ISolutionItemDescriptor> ();
        public ISolutionItemDescriptor[] EntryDescriptors
        {
            get { return entryDescriptors.ToArray(); }
        }

        public static SolutionDescriptor CreateSolutionDescriptor (XmlElement xmlElement)
        {
            SolutionDescriptor solutionDescriptor = new SolutionDescriptor ();

            if (xmlElement.Attributes["name"] != null)
                solutionDescriptor.name = xmlElement.Attributes["name"].Value;
            else
                throw new InvalidOperationException ("Attribute 'name' not found");

            if (xmlElement.Attributes["type"] != null)
                solutionDescriptor.type = xmlElement.Attributes["type"].Value;

            if (xmlElement.Attributes["directory"] != null)
                solutionDescriptor.directory = xmlElement.Attributes["directory"].Value;

            if (xmlElement["Options"] != null && xmlElement["Options"]["StartupProject"] != null)
                solutionDescriptor.startupProject = xmlElement["Options"]["StartupProject"].InnerText;


            foreach (XmlNode xmlNode in xmlElement.ChildNodes) {
                if (xmlNode is XmlElement) {
                    XmlElement xmlNodeElement = (XmlElement)xmlNode;
                    switch (xmlNodeElement.Name) {
                        case "Project":
                            solutionDescriptor.entryDescriptors.Add (ProjectDescriptor.CreateProjectDescriptor (xmlNodeElement));
                            break;
                        case "CombineEntry":
                        case "SolutionItem":
                            solutionDescriptor.entryDescriptors.Add (SolutionItemDescriptor.CreateDescriptor (xmlNodeElement));
                            break;

                    }
                }
            }

            return solutionDescriptor;
        }

        public WorkspaceItem CreateEntry (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
        {
            WorkspaceItem workspaceItem = null;

            if (string.IsNullOrEmpty (type))
                workspaceItem = new Solution ();
            else {
                Type workspaceItemType = Type.GetType (type);
                if (workspaceItemType != null)
                    workspaceItem = Activator.CreateInstance (workspaceItemType) as WorkspaceItem;

                if (workspaceItem == null) {
                    MessageService.ShowError (GettextCatalog.GetString ("Can't create solution with type: {0}", type));
					return null;
				}
            }

            workspaceItem.Name = StringParserService.Parse (name, new string[,] { {"ProjectName", projectCreateInformation.SolutionName} });

            workspaceItem.SetLocation (projectCreateInformation.SolutionPath, workspaceItem.Name);

            ProjectCreateInformation localProjectCI;
            if (!string.IsNullOrEmpty (directory) && directory != ".") {
                localProjectCI = new ProjectCreateInformation (projectCreateInformation);

                localProjectCI.SolutionPath = Path.Combine (localProjectCI.SolutionPath, directory);
                localProjectCI.ProjectBasePath = Path.Combine (localProjectCI.ProjectBasePath, directory);

                if (!Directory.Exists (localProjectCI.SolutionPath))
                    Directory.CreateDirectory (localProjectCI.SolutionPath);

                if (!Directory.Exists (localProjectCI.ProjectBasePath))
                    Directory.CreateDirectory (localProjectCI.ProjectBasePath);
            }
            else
                localProjectCI = projectCreateInformation;

            Solution solution = workspaceItem as Solution;
            if (solution != null) {
                for ( int i = 0; i < entryDescriptors.Count; i++ )
                {
                    ISolutionItemDescriptor solutionItem = entryDescriptors[i];

                    SolutionEntityItem info = solutionItem.CreateItem (localProjectCI, defaultLanguage);
                    entryDescriptors[i].InitializeItem (solution.RootFolder, localProjectCI, defaultLanguage, info);

                    IConfigurationTarget configurationTarget = info as IConfigurationTarget;
                    if (configurationTarget != null) {
                        foreach (ItemConfiguration configuration in configurationTarget.Configurations) {
                            bool flag = false;
                            foreach (SolutionConfiguration solutionCollection in solution.Configurations) {
                                if (solutionCollection.Id == configuration.Id)
                                    flag = true;
                            }
                            if (!flag)
                                solution.AddConfiguration (configuration.Id, true);
                        }
                    }

                    solution.RootFolder.Items.Add (info);
					if (startupProject == info.Name)
						solution.StartupItem = info;
                }
            }

            return workspaceItem;
        }
	}
}
