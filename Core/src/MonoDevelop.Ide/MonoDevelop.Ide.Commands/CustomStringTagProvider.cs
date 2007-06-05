// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Commands
{
	internal class SharpDevelopStringTagProvider :  IStringTagProvider 
	{
		public string[] Tags {
			get {
				return new string[] { "ITEMPATH", "ITEMDIR", "ITEMFILENAME", "ITEMEXT",
				                      "CURLINE", "CURCOL", "CURTEXT",
				                      "TARGETPATH", "TARGETDIR", "TARGETNAME", "TARGETEXT",
				                      "PROJECTDIR", "PROJECTFILENAME",
				                      "COMBINEDIR", "COMBINEFILENAME",
				                      "STARTUPPATH"};
			}
		}
		
		string GetCurrentItemPath()
		{
			if (IdeApp.Workbench.ActiveDocument != null && !IdeApp.Workbench.ActiveDocument.IsViewOnly && !IdeApp.Workbench.ActiveDocument.IsUntitled) {
				return IdeApp.Workbench.ActiveDocument.FileName;
			}
			return String.Empty;
		}
		
		string GetCurrentTargetPath()
		{
			if (ProjectService.ActiveProject != null) {
				return ProjectService.GetOutputFileName (ProjectService.ActiveProject.Project);
			}
			/* TODO: Project Conversion. btw. is this needed ? 
                     Should never happen because of the active project logic.
			if (IdeApp.Workbench.ActiveDocument != null) {
				string fileName = IdeApp.Workbench.ActiveDocument.FileName;
				IProject project = IdeApp.ProjectOperations.CurrentOpenCombine.FindProject (fileName);
				if (project != null) 
					return project.GetOutputFileName();
			}*/
			return String.Empty;
		}
		
		public string Convert(string tag)
		{
			switch (tag) {
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
					if (ProjectService.ActiveProject != null) {
						return ProjectService.ActiveProject.Project.BasePath;
					}
					break;
				case "PROJECTFILENAME":
					if (ProjectService.ActiveProject != null) {
						try {
							return Path.GetFileName (ProjectService.ActiveProject.Project.FileName);
						} catch (Exception) {}
					}
					break;
				
				case "COMBINEDIR":
					return ProjectService.Solution != null ? Path.GetDirectoryName (ProjectService.SolutionFileName) : "";

				case "COMBINEFILENAME":
					try {
						return ProjectService.Solution != null ? Path.GetFileName (ProjectService.SolutionFileName) : "";
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
