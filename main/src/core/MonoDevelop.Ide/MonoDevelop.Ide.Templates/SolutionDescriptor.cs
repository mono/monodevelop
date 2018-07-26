// 
// SolutionDescriptor.cs
//  
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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


using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;
using System.Xml;
using Mono.Addins;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Ide.Templates
{
    internal class SolutionDescriptor
	{
        string startupProject;
        string directory;
        string name;
        string type;
		RuntimeAddin addin;

        private List<ISolutionItemDescriptor> entryDescriptors = new List<ISolutionItemDescriptor> ();
        public ISolutionItemDescriptor[] EntryDescriptors
        {
            get { return entryDescriptors.ToArray(); }
        }

        public static SolutionDescriptor CreateSolutionDescriptor (RuntimeAddin addin, XmlElement xmlElement,
			FilePath baseDirectory)
        {
            SolutionDescriptor solutionDescriptor = new SolutionDescriptor ();
			solutionDescriptor.addin = addin;

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
                        solutionDescriptor.entryDescriptors.Add (
							ProjectDescriptor.CreateProjectDescriptor (xmlNodeElement, baseDirectory));
                        break;
                    case "CombineEntry":
                    case "SolutionItem":
                        solutionDescriptor.entryDescriptors.Add (
							SolutionItemDescriptor.CreateDescriptor (addin, xmlNodeElement));
                        break;
                    }
                }
            }

            return solutionDescriptor;
        }

		public async Task<WorkspaceItemCreatedInformation> CreateEntry (ProjectCreateInformation projectCreateInformation, string defaultLanguage)
        {
            WorkspaceItem workspaceItem = null;

            if (string.IsNullOrEmpty (type))
                workspaceItem = new Solution ();
            else {
                Type workspaceItemType = addin.GetType (type, false);
                if (workspaceItemType != null)
                    workspaceItem = Activator.CreateInstance (workspaceItemType) as WorkspaceItem;

                if (workspaceItem == null) {
                    MessageService.ShowError (GettextCatalog.GetString ("Can't create solution with type: {0}", type));
					return null;
				}
            }

			var substitution = new string[,] { { "ProjectName", projectCreateInformation.SolutionName } };
            workspaceItem.Name = StringParserService.Parse (name, substitution);
			string newStartupProjectName = startupProject;
			if (newStartupProjectName != null) {
				newStartupProjectName = StringParserService.Parse (startupProject, substitution);
			}

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

			var workspaceItemCreatedInfo = new WorkspaceItemCreatedInformation (workspaceItem);

            Solution solution = workspaceItem as Solution;
            if (solution != null) {
                for ( int i = 0; i < entryDescriptors.Count; i++ ) {
                    ProjectCreateInformation entryProjectCI;
                    var entry = entryDescriptors[i] as ICustomProjectCIEntry;
					if (entry != null) {
						entryProjectCI = entry.CreateProjectCI (localProjectCI);
						entryProjectCI = new ProjectTemplateCreateInformation (entryProjectCI, localProjectCI.ProjectName);
					} else
	                    entryProjectCI = localProjectCI;

					var solutionItemDesc = entryDescriptors[i];

					SolutionItem info = solutionItemDesc.CreateItem (entryProjectCI, defaultLanguage);
					if (info == null)
						continue;

					await solutionItemDesc.InitializeItem (solution.RootFolder, entryProjectCI, defaultLanguage, info);

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

					if ((info is Project) && (solutionItemDesc is ProjectDescriptor)) {
						workspaceItemCreatedInfo.AddPackageReferenceForCreatedProject ((Project)info, (ProjectDescriptor)solutionItemDesc, projectCreateInformation);
					}
                    solution.RootFolder.Items.Add (info);
					if (newStartupProjectName == info.Name)
						solution.StartupItem = info;
                }
            }

			var sol = workspaceItem as Solution;

			if (sol != null && !sol.SupportsFormat (sol.FileFormat)) {
				// The default format can't write solutions of this type. Find a compatible format.
				var f = MSBuildFileFormat.GetSupportedFormats ().First (ff => ff.CanWriteFile (sol));
				sol.ConvertToFormat (f);
			}
			
			return workspaceItemCreatedInfo;
        }

		public bool HasPackages ()
		{
			return entryDescriptors.OfType<ProjectDescriptor> ().Any (descriptor => descriptor.HasPackages ());
		}
	}
}
