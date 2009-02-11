//  CustomStringTagProvider.cs
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
using System.Drawing;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Commands
{
	internal class SharpDevelopStringTagProvider :  StringParserService.IStringTagProvider 
	{
		public IEnumerable<string> Tags {
			get {
				return new string[] { "ITEMPATH", "ITEMDIR", "ITEMFILENAME", "ITEMEXT",
				                      "CURLINE", "CURCOL", "CURTEXT",
				                      "TARGETPATH", "TARGETDIR", "TARGETNAME", "TARGETEXT",
				                      "PROJECTDIR", "PROJECTFILENAME",
				                      "COMBINEDIR", "COMBINEFILENAME",
				                      "SOLUTIONDIR", "SOLUTIONFILE",
				                      "STARTUPPATH"};
			}
		}
		
		string GetCurrentItemPath()
		{
			if (IdeApp.Workbench.ActiveDocument != null && !IdeApp.Workbench.ActiveDocument.IsViewOnly) {
				return IdeApp.Workbench.ActiveDocument.Name;
			}
			return String.Empty;
		}
		
		string GetCurrentTargetPath()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				return IdeApp.ProjectOperations.CurrentSelectedProject.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
			}
			if (IdeApp.Workbench.ActiveDocument != null) {
				Project project = IdeApp.Workbench.ActiveDocument.Project;
				if (project != null)
					return project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
			}
			return String.Empty;
		}
		
		public string Convert(string tag, string format)
		{
			switch (tag.ToUpper ()) {
				case "ITEMPATH":
					try {
						return GetCurrentItemPath();
					} catch (Exception) {}
					break;
				case "ITEMDIR":
					try {
						return Path.GetDirectoryName(GetCurrentItemPath());
					} catch (Exception) {}
					break;
				case "ITEMFILENAME":
					try {
						return Path.GetFileName(GetCurrentItemPath());
					} catch (Exception) {}
					break;
				case "ITEMEXT":
					try {
						return Path.GetExtension(GetCurrentItemPath());
					} catch (Exception) {}
					break;
				
				// TODO:
				case "CURLINE":
					return String.Empty;
				case "CURCOL":
					return String.Empty;
				case "CURTEXT":
					return String.Empty;
				
				case "TARGETPATH":
					try {
						return GetCurrentTargetPath();
					} catch (Exception) {}
					break;
				case "TARGETDIR":
					try {
						return Path.GetDirectoryName(GetCurrentTargetPath());
					} catch (Exception) {}
					break;
				case "TARGETNAME":
					try {
						return Path.GetFileName(GetCurrentTargetPath());
					} catch (Exception) {}
					break;
				case "TARGETEXT":
					try {
						return Path.GetExtension(GetCurrentTargetPath());
					} catch (Exception) {}
					break;
				
				case "PROJECTDIR":
					if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
						return IdeApp.ProjectOperations.CurrentSelectedProject.BaseDirectory;
					}
					break;
				case "PROJECTFILENAME":
					if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
						try {
							return Path.GetFileName(IdeApp.ProjectOperations.CurrentSelectedProject.FileName);
						} catch (Exception) {}
					}
					break;
				
				case "SOLUTIONDIR":
				case "COMBINEDIR":
					if (IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null)
						return Path.GetDirectoryName (IdeApp.ProjectOperations.CurrentSelectedSolutionItem.ParentSolution.FileName);
					break;

				case "SOLUTIONFILE":
				case "COMBINEFILENAME":
					try {
					if (IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null)
						return Path.GetFileName (IdeApp.ProjectOperations.CurrentSelectedSolutionItem.ParentSolution.FileName);
					} catch (Exception) {}
					break;
				case "STARTUPPATH":
					//return Application.StartupPath;
					return "";
			}
			return String.Empty;
		}
	}

}
