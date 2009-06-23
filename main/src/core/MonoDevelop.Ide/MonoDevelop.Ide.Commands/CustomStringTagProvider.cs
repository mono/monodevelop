// CustomStringTagProvider.cs
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

namespace MonoDevelop.Ide.Commands
{
    class DefaultStringTagProvider : StringParserService.IStringTagProvider
	{
        public IEnumerable<string> Tags { 
            get {
                return new String[] {
                                "ITEMPATH", 
                                "ITEMDIR", 
                                "ITEMFILENAME", 
                                "ITEMEXT", 
                                "TARGETPATH", 
                                "TARGETDIR", 
                                "TARGETNAME", 
                                "TARGETEXT", 
                                "PROJECTDIR", 
                                "PROJECTFILENAME",
                                "SOLUTIONDIR", 
                                "SOLUTIONFILE",
                                "COMBINEDIR", 
                                "COMBINEFILENAME"
                };
            }
        }

        public string Convert (string tag, string format)
        {
			try {
				switch (tag.ToUpperInvariant ()) {
					case "ITEMPATH":
						if (IdeApp.Workbench.ActiveDocument != null)
							return (IdeApp.Workbench.ActiveDocument.IsViewOnly) ? String.Empty : IdeApp.Workbench.ActiveDocument.Name;
						return String.Empty;

					case "ITEMDIR":
						if (IdeApp.Workbench.ActiveDocument != null)
							return (IdeApp.Workbench.ActiveDocument.IsViewOnly) ? String.Empty : (string)IdeApp.Workbench.ActiveDocument.FileName.ParentDirectory;
						return String.Empty;

					case "ITEMFILENAME":
						if (IdeApp.Workbench.ActiveDocument != null)
							return (IdeApp.Workbench.ActiveDocument.IsViewOnly) ? String.Empty : IdeApp.Workbench.ActiveDocument.FileName.FileName;
						return String.Empty;

					case "ITEMEXT":
						if (IdeApp.Workbench.ActiveDocument != null)
							return (IdeApp.Workbench.ActiveDocument.IsViewOnly) ? String.Empty : IdeApp.Workbench.ActiveDocument.FileName.Extension;
						return String.Empty;

					case "TARGETPATH":
						if (IdeApp.ProjectOperations.CurrentSelectedProject != null)
							return IdeApp.ProjectOperations.CurrentSelectedProject.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
						else
							if ((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.Project != null))
								return IdeApp.Workbench.ActiveDocument.Project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
						return String.Empty;

					case "TARGETDIR":
						if(IdeApp.ProjectOperations.CurrentSelectedProject != null)
							return Path.GetDirectoryName (IdeApp.ProjectOperations.CurrentSelectedProject.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
						else
							if((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.Project != null))
								return Path.GetDirectoryName (IdeApp.Workbench.ActiveDocument.Project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
						return String.Empty;

					case "TARGETNAME":
						if(IdeApp.ProjectOperations.CurrentSelectedProject != null)
							return Path.GetFileName (IdeApp.ProjectOperations.CurrentSelectedProject.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
						else
							if((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.Project != null))
								return Path.GetFileName (IdeApp.Workbench.ActiveDocument.Project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
						return String.Empty;

					case "TARGETEXT":
						if(IdeApp.ProjectOperations.CurrentSelectedProject != null)
							return Path.GetExtension (IdeApp.ProjectOperations.CurrentSelectedProject.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
						else
							if((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.Project != null))
								return Path.GetExtension (IdeApp.Workbench.ActiveDocument.Project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration));
						return String.Empty;

					case "PROJECTDIR":
						if((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.Project != null))
							return IdeApp.Workbench.ActiveDocument.Project.FileName.ParentDirectory.FileName;
						return String.Empty;

					case "PROJECTFILENAME":
						if((IdeApp.Workbench.ActiveDocument != null) && (IdeApp.Workbench.ActiveDocument.Project != null))
							return IdeApp.Workbench.ActiveDocument.Project.FileName.FileName;
						return String.Empty;

					case "SOLUTIONDIR":
					case "COMBINEDIR":
						if(IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null)
							return IdeApp.ProjectOperations.CurrentSelectedSolutionItem.ParentSolution.FileName.ParentDirectory.FileName;
						return String.Empty;

					case "SOLUTIONFILE":
					case "COMBINEFILENAME":
						if(IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null)
							return IdeApp.ProjectOperations.CurrentSelectedSolutionItem.ParentSolution.FileName.FileName;
						return String.Empty;

					default:
						return String.Empty;
				}
			}
			catch {
				return String.Empty;
			}
        }
	}
}
